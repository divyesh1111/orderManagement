namespace BlazorFrontend.Models;

public sealed record OrderDetailsDto(Guid OrderId, DateTime CreatedAtUtc, string Status, OrderDetailsCustomerDto Customer, IReadOnlyList<OrderDetailsItemDto> Items);

public sealed record OrderDetailsCustomerDto(Guid Id, string FirstName, string LastName, string Email);

public sealed record OrderDetailsItemDto(OrderDetailsProductDto Product, int Quantity, decimal UnitPriceSnapshot);

public sealed record OrderDetailsProductDto(Guid Id, string Name, decimal Price);