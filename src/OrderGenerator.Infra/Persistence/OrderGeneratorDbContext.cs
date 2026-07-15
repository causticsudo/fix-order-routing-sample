using Microsoft.EntityFrameworkCore;
using OrderGenerator.Domain.Aggregates;

namespace OrderGenerator.Infra.Persistence;

public sealed class OrderGeneratorDbContext : DbContext
{
    public OrderGeneratorDbContext(DbContextOptions<OrderGeneratorDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderEvent> OrderEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //todo: jogar num OrderMapping
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id)
                .ValueGeneratedNever();

            builder.Property(o => o.Symbol)
                .HasConversion(ValueConverters.SymbolConverter())
                .HasMaxLength(10);

            builder.Property(o => o.Side)
                .HasConversion(ValueConverters.OrderSideConverter())
                .HasMaxLength(10);

            builder.Property(o => o.Quantity)
                .HasConversion(ValueConverters.QuantityConverter());

            builder.Property(o => o.Price)
                .HasConversion(ValueConverters.PriceConverter())
                .HasPrecision(18, 2);

            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(o => o.RejectionReason)
                .HasMaxLength(1000);

            builder.Property(o => o.CreatedAt);
            builder.Property(o => o.UpdatedAt);

            builder.HasIndex(o => o.CreatedAt);
            builder.HasIndex(o => o.Symbol);
        });

        modelBuilder.Entity<OrderEvent>(builder =>
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.OrderId);

            builder.Property(e => e.CorrelationKey)
                .HasMaxLength(100);

            builder.Property(e => e.EventType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(e => e.Details)
                .HasMaxLength(1000);

            builder.Property(e => e.OccurredAt);

            builder.HasIndex(e => e.OrderId);
            builder.HasIndex(e => e.CorrelationKey);
            builder.HasIndex(e => e.OccurredAt);
        });
    }
}
