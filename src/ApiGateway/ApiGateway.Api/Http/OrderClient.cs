using System.Text.Json;
using ApiGateway.Api.Dtos;

namespace ApiGateway.Api.Http;

public sealed class OrderClient(HttpClient httpClient)
{
    public async Task<OrderReadDto?> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/orders/{orderId}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<OrderReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

public sealed record OrderReadDto(Guid Id, Guid CustomerId, DateTime CreatedAtUtc, string Status, IReadOnlyList<OrderItemReadDto> Items);

public sealed record OrderItemReadDto(Guid ProductId, int Quantity, decimal UnitPriceSnapshot);