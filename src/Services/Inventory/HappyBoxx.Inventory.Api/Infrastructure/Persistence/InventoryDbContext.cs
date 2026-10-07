using HappyBoxx.Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Inventory.Api.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<StockItem> StockItems => Set<StockItem>();

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
        builder.Property(s => s.ReorderLevel).HasPrecision(18, 3);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.IsLowStock);

        builder.HasIndex(s => s.ProductId).IsUnique();
        builder.HasIndex(s => s.Sku);
    }
}
