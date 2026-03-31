namespace ApiGateway.Api.Dtos;

public sealed record OrderDetailsDto(Guid OrderId, DateTime CreatedAtUtc, string Status, OrderDetailsCustomerDto Customer, IReadOnlyList<OrderDetailsItemDto> Items);