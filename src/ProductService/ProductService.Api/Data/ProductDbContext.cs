using Microsoft.EntityFrameworkCore;
using ProductService.Api.Entities;

namespace ProductService.Api.Data;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Stock).IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EventId).IsUnique();
            b.Property(x => x.ProcessedAtUtc).IsRequired();
        });
    }
}
