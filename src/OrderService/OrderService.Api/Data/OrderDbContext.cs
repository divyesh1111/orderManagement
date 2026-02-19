using Microsoft.EntityFrameworkCore;
using OrderService.Api.Entities;

namespace OrderService.Api.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2).IsRequired();
        });
    }
}
