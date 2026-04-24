using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public sealed class ProductsApi(HttpClient http)
{
    public async Task<List<ProductReadDto>> GetAllAsync(CancellationToken ct)
    {
        var data = await http.GetFromJsonAsync<List<ProductReadDto>>("/api/products", ct);
        return data ?? [];
    }

    public async Task<ProductReadDto?> CreateAsync(ProductCreateDto dto, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("/api/products", dto, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ProductReadDto>(cancellationToken: ct);
    }
}