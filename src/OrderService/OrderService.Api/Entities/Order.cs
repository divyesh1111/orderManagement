namespace OrderService.Api.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Status { get; set; } = "Created";
    public List<OrderItem> Items { get; set; } = [];
}
