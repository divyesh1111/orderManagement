using Microsoft.EntityFrameworkCore;
using NotificationService.Api.Data;
using NotificationService.Api.Dtos;

namespace NotificationService.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications");

        group.MapGet("/", async (NotificationDbContext db) =>
        {
            var items = await db.Notifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new NotificationReadDto(x.Id, x.OrderId, x.Message, x.CreatedAtUtc))
                .ToListAsync();

            return Results.Ok(items);
        });

        return app;
    }
}
