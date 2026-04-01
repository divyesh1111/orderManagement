using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public sealed class NotificationsApi(HttpClient http)
{
    public async Task<List<NotificationReadDto>> GetAllAsync(CancellationToken ct)
    {
        var data = await http.GetFromJsonAsync<List<NotificationReadDto>>("/api/notifications", ct);
        return data ?? [];
    }
}