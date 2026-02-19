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
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (evt is null)
            {
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var alreadyProcessed = await db.ProcessedEvents.AnyAsync(x => x.EventId == evt.EventId);
            if (alreadyProcessed)
            {
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var productIds = evt.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            foreach (var item in evt.Items)
            {
                var product = products.SingleOrDefault(x => x.Id == item.ProductId);
                if (product is null) continue;
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
            }

            await db.ProcessedEvents.AddAsync(new ProcessedEvent
            {
                Id = Guid.NewGuid(),
                EventId = evt.EventId,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch
        {
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    public override void Dispose()
    {
        channel?.Close();
        connection?.Close();
        base.Dispose();
    }
}
