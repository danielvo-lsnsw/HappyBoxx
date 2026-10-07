using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Catalog.Api.Domain.Categories;

public sealed class Category : AuditableEntity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public static Category Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, string? description, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description?.Trim();
        IsActive = isActive;
    }
}
