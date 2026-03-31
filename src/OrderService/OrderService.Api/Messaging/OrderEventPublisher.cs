using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderService.Api.Messaging;

public sealed class OrderEventPublisher(IOptions<RabbitMqOptions> optionsAccessor)
{
    readonly RabbitMqOptions options = optionsAccessor.Value;

    public void PublishOrderCreated(OrderCreatedEvent evt) => Publish(evt, options.OrderCreatedRoutingKey);

    public void PublishOrderCancelled(OrderCancelledEvent evt) => Publish(evt, options.OrderCancelledRoutingKey);

    void Publish<T>(T evt, string routingKey)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.User,
            Password = options.Pass
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);

        var payload = JsonSerializer.Serialize(evt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var body = Encoding.UTF8.GetBytes(payload);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;

        channel.BasicPublish(
            exchange: options.Exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body
        );
    }
}