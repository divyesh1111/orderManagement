namespace ApiGateway.Api.Dtos;

public sealed record OrderDetailsItemDto(OrderDetailsProductDto Product, int Quantity, decimal UnitPriceSnapshot);