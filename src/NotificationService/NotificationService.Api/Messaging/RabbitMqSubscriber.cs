using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.Options;
using NotificationService.Api.Data;
using NotificationService.Api.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Api.Messaging;

public sealed class RabbitMqSubscriber(
    IServiceProvider serviceProvider,
    IOptions<RabbitMqOptions> optionsAccessor) : BackgroundService
{
    readonly RabbitMqOptions options = optionsAccessor.Value;

    IConnection? connection;
    IModel? channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsumer();
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                Cleanup();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    void StartConsumer()
    {
        Cleanup();

        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.User,
            Password = options.Pass,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
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
    }

    static string ItemsText(IReadOnlyList<OrderCreatedItemDto> items)
    {
        if (items.Count == 0) return "None";
        return string.Join(", ", items.Select(x => $"{x.ProductId} x{x.Quantity}"));
    }

    async Task OnMessage(object sender, BasicDeliverEventArgs ea)
    {
        if (channel is null) return;

        try
        {
            var routingKey = ea.RoutingKey ?? "";
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            Guid orderId;
            string message;

            if (routingKey == options.OrderCreatedRoutingKey)
            {
                var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (evt is null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                orderId = evt.OrderId;
                message = $"Order created. Items: {ItemsText(evt.Items)}";
            }
            else if (routingKey == options.OrderCancelledRoutingKey)
            {
                var evt = JsonSerializer.Deserialize<OrderCancelledEvent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (evt is null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                orderId = evt.OrderId;
                message = $"Order cancelled. Items: {ItemsText(evt.Items)}";
            }
            else
            {
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            await db.Notifications.AddAsync(new NotificationLog
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch
        {
            try
            {
                channel?.BasicNack(ea.DeliveryTag, false, true);
            }
            catch
            {
            }
        }
    }

    void Cleanup()
    {
        try { channel?.Close(); } catch { }
        try { connection?.Close(); } catch { }
        channel?.Dispose();
        connection?.Dispose();
        channel = null;
        connection = null;
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }
}