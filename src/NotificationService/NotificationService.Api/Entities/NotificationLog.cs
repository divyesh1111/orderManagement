namespace NotificationService.Api.Entities;

public sealed class NotificationLog
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
