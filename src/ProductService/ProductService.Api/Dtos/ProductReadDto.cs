namespace ProductService.Api.Dtos;

public sealed record ProductReadDto(Guid Id, string Name, decimal Price, int Stock);
