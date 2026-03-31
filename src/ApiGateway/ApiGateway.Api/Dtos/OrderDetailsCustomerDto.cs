namespace ApiGateway.Api.Dtos;

public sealed record OrderDetailsCustomerDto(Guid Id, string FirstName, string LastName, string Email);