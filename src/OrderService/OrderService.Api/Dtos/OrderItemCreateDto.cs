namespace OrderService.Api.Dtos;

public sealed record OrderItemCreateDto(Guid ProductId, int Quantity);
