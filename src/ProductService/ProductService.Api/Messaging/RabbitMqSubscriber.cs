using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductService.Api.Data;
using ProductService.Api.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProductService.Api.Messaging;

public sealed class RabbitMqSubscriber(
    IServiceProvider serviceProvider,
    IOptions<RabbitMqOptions> optionsAccessor) : BackgroundService
{
    readonly RabbitMqOptions options = optionsAccessor.Value;
    IConnection? connection;
    IModel? channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.User,
            Password = options.Pass,
            DispatchConsumersAsync = true
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        channel.ExchangeDeclare(options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        channel.QueueDeclare(options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        channel.QueueBind(options.QueueName, options.Exchange, options.OrderCreatedRoutingKey);
        channel.QueueBind(options.QueueName, options.Exchange, options.OrderCancelledRoutingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessage;

        channel.BasicConsume(queue: options.QueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    async Task OnMessage(object sender, BasicDeliverEventArgs ea)
    {
        if (channel is null) return;

        try
        {
            var routingKey = ea.RoutingKey ?? "";
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            if (routingKey == options.OrderCreatedRoutingKey)
            {
                var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (evt is null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var alreadyProcessed = await db.ProcessedEvents.AnyAsync(x => x.EventId == evt.EventId);
                if (alreadyProcessed)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await ApplyStockDelta(db, evt.Items, -1);

                await db.ProcessedEvents.AddAsync(new ProcessedEvent
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            if (routingKey == options.OrderCancelledRoutingKey)
            {
                var evt = JsonSerializer.Deserialize<OrderCancelledEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (evt is null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var alreadyProcessed = await db.ProcessedEvents.AnyAsync(x => x.EventId == evt.EventId);
                if (alreadyProcessed)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await ApplyStockDelta(db, evt.Items, 1);

                await db.ProcessedEvents.AddAsync(new ProcessedEvent
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch
        {
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    static async Task ApplyStockDelta(ProductDbContext db, IReadOnlyList<OrderCreatedItemDto> items, int sign)
    {
        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

        foreach (var item in items)
        {
            var product = products.SingleOrDefault(x => x.Id == item.ProductId);
            if (product is null) continue;

            if (sign < 0)
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
            else
                product.Stock = product.Stock + item.Quantity;
        }
    }

    public override void Dispose()
    {
        channel?.Close();
        connection?.Close();
        base.Dispose();
    }
}