namespace CustomerService.Api.Dtos;

public sealed record CustomerReadDto(Guid Id, string FirstName, string LastName, string Email);
