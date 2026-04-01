using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public sealed class CustomersApi(HttpClient http)
{
    public async Task<List<CustomerReadDto>> GetAllAsync(CancellationToken ct)
    {
        var data = await http.GetFromJsonAsync<List<CustomerReadDto>>("/api/customers", ct);
        return data ?? [];
    }

    public async Task<CustomerReadDto?> CreateAsync(CustomerCreateDto dto, CancellationToken ct)
    {
        var res = await http.PostAsJsonAsync("/api/customers", dto, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<CustomerReadDto>(cancellationToken: ct);
    }
}
