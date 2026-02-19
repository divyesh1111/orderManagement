using Microsoft.EntityFrameworkCore;
using NotificationService.Api.Entities;

namespace NotificationService.Api.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.Message).HasMaxLength(500).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}
