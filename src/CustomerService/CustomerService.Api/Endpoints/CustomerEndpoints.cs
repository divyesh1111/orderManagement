using CustomerService.Api.Data;
using CustomerService.Api.Dtos;
using CustomerService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers");

        group.MapGet("/", async (CustomerDbContext db) =>
        {
            var items = await db.Customers
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .Select(x => new CustomerReadDto(x.Id, x.FirstName, x.LastName, x.Email))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:guid}", async (Guid id, CustomerDbContext db) =>
        {
            var item = await db.Customers
                .Where(x => x.Id == id)
                .Select(x => new CustomerReadDto(x.Id, x.FirstName, x.LastName, x.Email))
                .SingleOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapGet("/{id:guid}/exists", async (Guid id, CustomerDbContext db) =>
        {
            var exists = await db.Customers.AnyAsync(x => x.Id == id);
            return Results.Ok(new { exists });
        });

        group.MapPost("/", async (CustomerCreateDto dto, CustomerDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName)) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(dto.LastName)) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(dto.Email)) return Results.BadRequest();

            var entity = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant()
            };

            await db.Customers.AddAsync(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/customers/{entity.Id}", new CustomerReadDto(entity.Id, entity.FirstName, entity.LastName, entity.Email));
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomerUpdateDto dto, CustomerDbContext db) =>
        {
            var entity = await db.Customers.SingleOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.FirstName)) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(dto.LastName)) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(dto.Email)) return Results.BadRequest();

            entity.FirstName = dto.FirstName.Trim();
            entity.LastName = dto.LastName.Trim();
            entity.Email = dto.Email.Trim().ToLowerInvariant();

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, CustomerDbContext db) =>
        {
            var entity = await db.Customers.SingleOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();

            db.Customers.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
