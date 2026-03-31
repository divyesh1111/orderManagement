using System.Text.Json;

namespace ApiGateway.Api.Http;

public sealed class CustomerClient(HttpClient httpClient)
{
    public async Task<CustomerReadDto?> GetCustomerAsync(Guid customerId, CancellationToken ct)
    {
        var res = await httpClient.GetAsync($"/customers/{customerId}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<CustomerReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

public sealed record CustomerReadDto(Guid Id, string FirstName, string LastName, string Email);