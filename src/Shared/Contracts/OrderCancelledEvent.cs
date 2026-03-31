namespace Contracts;

public sealed record OrderCancelledEvent(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    DateTime CancelledAtUtc,
    IReadOnlyList<OrderCreatedItemDto> Items
);