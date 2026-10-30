using HappyBoxx.Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Inventory.Api.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Sku).HasMaxLength(StockItem.SkuMaxLength).IsRequired();
        builder.Property(s => s.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(s => s.MinLevel).HasPrecision(18, 3);
        builder.Property(s => s.MaxLevel).HasPrecision(18, 3);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.IsLowStock);
        builder.Ignore(s => s.ReorderLevel);

        builder.HasIndex(s => s.ProductId).IsUnique();
        builder.HasIndex(s => s.Sku);
    }
}

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.QuantityChange).HasPrecision(18, 3);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.OccurredAtUtc });
    }
}
