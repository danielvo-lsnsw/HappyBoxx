using HappyBoxx.Catalog.Api.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(Category.DescriptionMaxLength);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
