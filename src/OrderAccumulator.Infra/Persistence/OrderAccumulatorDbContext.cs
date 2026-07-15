using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Aggregates;

namespace OrderAccumulator.Infra.Persistence;

public class OrderAccumulatorDbContext : DbContext
{
    public DbSet<OrderExecution> OrderExecutions { get; set; }

    public OrderAccumulatorDbContext(DbContextOptions<OrderAccumulatorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClOrdId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ExecutedAt).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasIndex(e => e.ClOrdId).IsUnique();
            entity.HasIndex(e => new { e.Symbol, e.ExecutedAt });
        });
    }
}
