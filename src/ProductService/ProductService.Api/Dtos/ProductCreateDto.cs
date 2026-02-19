namespace ProductService.Api.Dtos;

public sealed record ProductCreateDto(string Name, decimal Price, int Stock);
