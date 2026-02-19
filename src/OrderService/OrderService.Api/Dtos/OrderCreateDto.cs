namespace OrderService.Api.Dtos;

public sealed record OrderCreateDto(Guid CustomerId, List<OrderItemCreateDto> Items);
