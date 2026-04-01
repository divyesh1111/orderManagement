using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public sealed class OrdersApi(HttpClient http)
{
    public async Task<List<OrderReadDto>> GetAllAsync(CancellationToken ct)
    {
        var data = await http.GetFromJsonAsync<List<OrderReadDto>>("/api/orders", ct);
        return data ?? [];
    }

    public async Task<OrderReadDto?> CreateAsync(OrderCreateDto dto, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("/api/orders", dto, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderReadDto>(cancellationToken: ct);
    }

    public async Task<bool> CancelAsync(Guid orderId, CancellationToken ct)
    {
        var res = await http.PostAsync($"/api/orders/{orderId}/cancel", content: null, cancellationToken: ct);
        return res.IsSuccessStatusCode;
    }
}