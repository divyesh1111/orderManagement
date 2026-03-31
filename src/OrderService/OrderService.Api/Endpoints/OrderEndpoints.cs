using Contracts;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Dtos;
using OrderService.Api.Entities;
using OrderService.Api.Http;
using OrderService.Api.Messaging;

namespace OrderService.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapGet("/", async (OrderDbContext db) =>
        {
            var orders = await db.Orders
                .Include(x => x.Items)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();

            var mapped = orders.Select(o => new OrderReadDto(
                o.Id,
                o.CustomerId,
                o.CreatedAtUtc,
                o.Status,
                o.Items.Select(i => new OrderItemReadDto(i.ProductId, i.Quantity, i.UnitPriceSnapshot)).ToList()
            )).ToList();

            return Results.Ok(mapped);
        });

        group.MapGet("/{id:guid}", async (Guid id, OrderDbContext db) =>
        {
            var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound();

            var mapped = new OrderReadDto(
                order.Id,
                order.CustomerId,
                order.CreatedAtUtc,
                order.Status,
                order.Items.Select(i => new OrderItemReadDto(i.ProductId, i.Quantity, i.UnitPriceSnapshot)).ToList()
            );

            return Results.Ok(mapped);
        });

        group.MapPost("/", async (
            OrderCreateDto dto,
            OrderDbContext db,
            CustomerClient customers,
            ProductClient products,
            OrderEventPublisher publisher,
            CancellationToken ct) =>
        {
            if (dto.CustomerId == Guid.Empty) return Results.BadRequest();
            if (dto.Items is null || dto.Items.Count == 0) return Results.BadRequest();
            if (dto.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0)) return Results.BadRequest();

            var customerExists = await customers.ExistsAsync(dto.CustomerId, ct);
            if (!customerExists) return Results.BadRequest(new { error = "CustomerNotFound" });

            foreach (var item in dto.Items)
            {
                var productExists = await products.ExistsAsync(item.ProductId, ct);
                if (!productExists) return Results.BadRequest(new { error = "ProductNotFound", productId = item.ProductId });
            }

            var priceMap = new Dictionary<Guid, decimal>();
            foreach (var pid in dto.Items.Select(x => x.ProductId).Distinct())
            {
                var snapshot = await products.GetAsync(pid, ct);
                if (snapshot is null) return Results.BadRequest(new { error = "ProductNotFound", productId = pid });
                priceMap[pid] = snapshot.Price;
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Created"
            };

            order.Items = dto.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPriceSnapshot = priceMap[i.ProductId]
            }).ToList();

            await db.Orders.AddAsync(order, ct);
            await db.SaveChangesAsync(ct);

            var evt = new OrderCreatedEvent(
                EventId: Guid.NewGuid(),
                OrderId: order.Id,
                CustomerId: order.CustomerId,
                CreatedAtUtc: order.CreatedAtUtc,
                Items: order.Items.Select(i => new OrderCreatedItemDto(i.ProductId, i.Quantity)).ToList()
            );

            publisher.PublishOrderCreated(evt);

            var mapped = new OrderReadDto(
                order.Id,
                order.CustomerId,
                order.CreatedAtUtc,
                order.Status,
                order.Items.Select(i => new OrderItemReadDto(i.ProductId, i.Quantity, i.UnitPriceSnapshot)).ToList()
            );

            return Results.Created($"/orders/{order.Id}", mapped);
        });

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            OrderDbContext db,
            OrderEventPublisher publisher,
            CancellationToken ct) =>
        {
            var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound();
            if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) return Results.Conflict(new { error = "OrderAlreadyCancelled" });

            order.Status = "Cancelled";
            await db.SaveChangesAsync(ct);

            var evt = new OrderCancelledEvent(
                EventId: Guid.NewGuid(),
                OrderId: order.Id,
                CustomerId: order.CustomerId,
                CancelledAtUtc: DateTime.UtcNow,
                Items: order.Items.Select(i => new OrderCreatedItemDto(i.ProductId, i.Quantity)).ToList()
            );

            publisher.PublishOrderCancelled(evt);

            return Results.NoContent();
        });

        return app;
    }
}