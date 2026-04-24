namespace BlazorFrontend.Models;

public sealed record OrderItemCreateDto(Guid ProductId, int Quantity);

public sealed record OrderCreateDto(Guid CustomerId, List<OrderItemCreateDto> Items);

public sealed record OrderItemReadDto(Guid ProductId, int Quantity, decimal UnitPriceSnapshot);

public sealed record OrderReadDto(Guid Id, Guid CustomerId, DateTime CreatedAtUtc, string Status, IReadOnlyList<OrderItemReadDto> Items);