namespace ProductService.Api.Dtos;

public sealed record ProductUpdateDto(string Name, decimal Price, int Stock);
