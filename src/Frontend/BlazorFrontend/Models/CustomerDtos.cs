namespace BlazorFrontend.Models;

public sealed record CustomerReadDto(Guid Id, string FirstName, string LastName, string Email);

public sealed record CustomerCreateDto(string FirstName, string LastName, string Email);
