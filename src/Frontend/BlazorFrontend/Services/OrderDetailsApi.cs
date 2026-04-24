using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public sealed class OrderDetailsApi(HttpClient http)
{
    public async Task<OrderDetailsDto?> GetAsync(Guid orderId, CancellationToken ct)
    {
        var res = await http.GetAsync($"/api/orders/{orderId}/details", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: ct);
    }
}