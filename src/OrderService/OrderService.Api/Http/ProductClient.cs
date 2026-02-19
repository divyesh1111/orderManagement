using System.Text.Json;

namespace OrderService.Api.Http;

public sealed class ProductClient(HttpClient httpClient)
{
    public async Task<bool> ExistsAsync(Guid productId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/products/{productId}/exists", ct);
        if (!res.IsSuccessStatusCode) return false;
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("exists", out var p) && p.GetBoolean();
    }

    public async Task<ProductSnapshotDto?> GetAsync(Guid productId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/products/{productId}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ProductSnapshotDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
