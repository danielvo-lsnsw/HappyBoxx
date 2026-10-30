using HappyBoxx.Catalog.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProductChangeHistoryEntryConfiguration : IEntityTypeConfiguration<ProductChangeHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ProductChangeHistoryEntry> builder)
    {
        builder.ToTable("ProductChangeHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.ChangedBy)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.ChangedAtUtc).IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProductId, x.ChangedAtUtc });
    }
}
