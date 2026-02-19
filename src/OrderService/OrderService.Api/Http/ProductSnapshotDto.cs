namespace OrderService.Api.Http;

public sealed record ProductSnapshotDto(Guid Id, string Name, decimal Price, int Stock);
