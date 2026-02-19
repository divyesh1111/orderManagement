namespace Contracts;

public sealed record OrderCreatedEvent(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderCreatedItemDto> Items
);
