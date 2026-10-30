using HappyBoxx.BuildingBlocks.Domain;
using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Domain.Suppliers;

namespace HappyBoxx.Catalog.Api.Domain.Products;

public sealed class Product : AuditableEntity
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int PackDetailsMaxLength = 200;
    public const int LegacyCodeMaxLength = 50;

    private readonly List<ProductSupplier> _productSuppliers = [];

    private Product()
    {
    }

    public string Sku { get; private set; } = string.Empty;

    public string? LegacyCode { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public UnitOfMeasure Unit { get; private set; }

    public string? PackDetails { get; private set; }

    public decimal? SellPriceNear { get; private set; }

    public decimal? SellPriceFar { get; private set; }

    public decimal? BuyPrice { get; private set; }

    public DateOnly? PriceEffectiveDate { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ProductSupplier> ProductSuppliers => _productSuppliers;

    public static Product Create(
        string sku,
        string? legacyCode,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure unit,
        string? packDetails,
        decimal? sellPriceNear,
        decimal? sellPriceFar,
        decimal? buyPrice,
        DateOnly? priceEffectiveDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var product = new Product
        {
            Sku = sku.Trim().ToUpperInvariant(),
            IsActive = true,
        };
        product.SetDetails(
            legacyCode,
            name,
            description,
            categoryId,
            unit,
            packDetails,
            sellPriceNear,
            sellPriceFar,
            buyPrice,
            priceEffectiveDate);
        return product;
    }

    public void Update(
        string? legacyCode,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure unit,
        string? packDetails,
        decimal? sellPriceNear,
        decimal? sellPriceFar,
        decimal? buyPrice,
        DateOnly? priceEffectiveDate,
        bool isActive)
    {
        SetDetails(
            legacyCode,
            name,
            description,
            categoryId,
            unit,
            packDetails,
            sellPriceNear,
            sellPriceFar,
            buyPrice,
            priceEffectiveDate);
        IsActive = isActive;
    }

    public void SetSuppliers(IReadOnlyCollection<Guid> supplierIds, Guid? defaultSupplierId)
    {
        var target = supplierIds.Distinct().ToArray();
        _productSuppliers.RemoveAll(ps => !target.Contains(ps.SupplierId));

        foreach (var supplierId in target)
        {
            var existing = _productSuppliers.FirstOrDefault(ps => ps.SupplierId == supplierId);
            if (existing is null)
            {
                _productSuppliers.Add(ProductSupplier.Create(Id, supplierId, supplierId == defaultSupplierId));
                continue;
            }

            existing.SetDefault(supplierId == defaultSupplierId);
        }
    }

    public void CaptureDefaultSupplierLastBuyPrice(DateTimeOffset atUtc)
    {
        if (!BuyPrice.HasValue)
        {
            return;
        }

        var defaultSupplier = _productSuppliers.FirstOrDefault(ps => ps.IsDefault);
        defaultSupplier?.SetLastBuyPrice(BuyPrice.Value, atUtc);
    }

    private void SetDetails(
        string? legacyCode,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure unit,
        string? packDetails,
        decimal? sellPriceNear,
        decimal? sellPriceFar,
        decimal? buyPrice,
        DateOnly? priceEffectiveDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (sellPriceNear is { } nearPrice)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(nearPrice);
        }

        if (sellPriceFar is { } farPrice)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(farPrice);
        }

        if (buyPrice is { } purchasePrice)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(purchasePrice);
        }

        LegacyCode = string.IsNullOrWhiteSpace(legacyCode) ? null : legacyCode.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        Unit = unit;
        PackDetails = string.IsNullOrWhiteSpace(packDetails) ? null : packDetails.Trim();
        SellPriceNear = sellPriceNear;
        SellPriceFar = sellPriceFar;
        BuyPrice = buyPrice;
        PriceEffectiveDate = priceEffectiveDate;
    }
}
