namespace NotificationService.Api.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Exchange { get; set; } = "orders.exchange";
    public string OrderCreatedRoutingKey { get; set; } = "order.created";
    public string OrderCancelledRoutingKey { get; set; } = "order.cancelled";
    public string QueueName { get; set; } = "notification.ordercreated";
}