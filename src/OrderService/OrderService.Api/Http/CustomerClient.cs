using System.Text.Json;

namespace OrderService.Api.Http;

public sealed class CustomerClient(HttpClient httpClient)
{
    public async Task<bool> ExistsAsync(Guid customerId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/customers/{customerId}/exists", ct);
        if (!res.IsSuccessStatusCode) return false;
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("exists", out var p) && p.GetBoolean();
    }
}
