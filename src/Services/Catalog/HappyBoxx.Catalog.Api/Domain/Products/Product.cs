using HappyBoxx.BuildingBlocks.Domain;
using HappyBoxx.Catalog.Api.Domain.Categories;

namespace HappyBoxx.Catalog.Api.Domain.Products;

public sealed class Product : AuditableEntity
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private Product()
    {
    }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public UnitOfMeasure Unit { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(
        string sku,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure unit,
        decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var product = new Product
        {
            Sku = sku.Trim().ToUpperInvariant(),
            IsActive = true,
        };
        product.SetDetails(name, description, categoryId, unit, price);
        return product;
    }

    public void Update(
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure unit,
        decimal price,
        bool isActive)
    {
        SetDetails(name, description, categoryId, unit, price);
        IsActive = isActive;
    }

    private void SetDetails(string name, string? description, Guid categoryId, UnitOfMeasure unit, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Name = name.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        Unit = unit;
        Price = price;
    }
}
