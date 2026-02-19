using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.Dtos;
using ProductService.Api.Entities;

namespace ProductService.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products");

        group.MapGet("/", async (ProductDbContext db) =>
        {
            var items = await db.Products
                .OrderBy(x => x.Name)
                .Select(x => new ProductReadDto(x.Id, x.Name, x.Price, x.Stock))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:guid}", async (Guid id, ProductDbContext db) =>
        {
            var item = await db.Products
                .Where(x => x.Id == id)
                .Select(x => new ProductReadDto(x.Id, x.Name, x.Price, x.Stock))
                .SingleOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapGet("/{id:guid}/exists", async (Guid id, ProductDbContext db) =>
        {
            var exists = await db.Products.AnyAsync(x => x.Id == id);
            return Results.Ok(new { exists });
        });

        group.MapPost("/", async (ProductCreateDto dto, ProductDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest();

            var entity = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Stock = dto.Stock
            };

            await db.Products.AddAsync(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/products/{entity.Id}", new ProductReadDto(entity.Id, entity.Name, entity.Price, entity.Stock));
        });

        group.MapPut("/{id:guid}", async (Guid id, ProductUpdateDto dto, ProductDbContext db) =>
        {
            var entity = await db.Products.SingleOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest();

            entity.Name = dto.Name.Trim();
            entity.Price = dto.Price;
            entity.Stock = dto.Stock;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ProductDbContext db) =>
        {
            var entity = await db.Products.SingleOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();

            db.Products.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
