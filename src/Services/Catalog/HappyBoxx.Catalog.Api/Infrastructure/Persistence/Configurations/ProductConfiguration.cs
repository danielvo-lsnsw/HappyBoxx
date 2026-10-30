using HappyBoxx.Catalog.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Sku).HasMaxLength(Product.SkuMaxLength).IsRequired();
        builder.Property(p => p.LegacyCode).HasMaxLength(Product.LegacyCodeMaxLength);
        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.Unit).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PackDetails).HasMaxLength(Product.PackDetailsMaxLength);
        builder.Property(p => p.SellPriceNear).HasPrecision(18, 3);
        builder.Property(p => p.SellPriceFar).HasPrecision(18, 3);
        builder.Property(p => p.BuyPrice).HasPrecision(18, 3);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.Name);
    }
}
