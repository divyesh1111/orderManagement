namespace ProductService.Api.Entities;

public sealed class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
