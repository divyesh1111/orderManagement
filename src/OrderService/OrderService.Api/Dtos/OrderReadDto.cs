namespace OrderService.Api.Dtos;

public sealed record OrderReadDto(Guid Id, Guid CustomerId, DateTime CreatedAtUtc, string Status, IReadOnlyList<OrderItemReadDto> Items);
