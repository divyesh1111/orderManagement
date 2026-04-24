namespace BlazorFrontend.Models;

public sealed record ProductReadDto(Guid Id, string Name, decimal Price, int Stock);

public sealed record ProductCreateDto(string Name, decimal Price, int Stock);