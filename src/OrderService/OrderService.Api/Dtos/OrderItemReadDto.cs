namespace OrderService.Api.Dtos;

public sealed record OrderItemReadDto(Guid ProductId, int Quantity, decimal UnitPriceSnapshot);
