using System.Text.Json;

namespace ApiGateway.Api.Http;

public sealed class ProductClient(HttpClient httpClient)
{
    public async Task<ProductReadDto?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/products/{productId}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ProductReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

public sealed record ProductReadDto(Guid Id, string Name, decimal Price, int Stock);