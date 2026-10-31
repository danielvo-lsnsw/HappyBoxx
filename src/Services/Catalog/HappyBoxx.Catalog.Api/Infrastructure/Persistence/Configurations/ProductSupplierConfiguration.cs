using HappyBoxx.Catalog.Api.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProductSupplierConfiguration : IEntityTypeConfiguration<ProductSupplier>
{
    public void Configure(EntityTypeBuilder<ProductSupplier> builder)
    {
        builder.ToTable("ProductSuppliers");
        builder.HasKey(ps => new { ps.ProductId, ps.SupplierId });

        builder.Property(ps => ps.IsDefault).IsRequired();
        builder.Property(ps => ps.LastBuyPrice).HasPrecision(18, 3);
        builder.Property(ps => ps.LastBuyPriceAtUtc);
        builder.Property(ps => ps.Note).HasMaxLength(ProductSupplier.NoteMaxLength);

        builder.HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSuppliers)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Supplier)
            .WithMany(s => s.ProductSuppliers)
            .HasForeignKey(ps => ps.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ps => new { ps.ProductId, ps.IsDefault })
            .HasFilter("[IsDefault] = 1");
    }
}
