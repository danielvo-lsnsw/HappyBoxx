using HappyBoxx.BuildingBlocks.Domain;
using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Inventory.Api.Domain;

public sealed class StockItem : AuditableEntity
{
    public const int SkuMaxLength = 50;

    private StockItem()
    {
    }

    /// <summary>Reference to the Catalog product (owned by the Catalog service – no FK across services).</summary>
    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public decimal QuantityOnHand { get; private set; }

    public decimal MinLevel { get; private set; }

    public decimal MaxLevel { get; private set; }

    public decimal ReorderLevel => MinLevel;

    public bool IsLowStock => QuantityOnHand <= MinLevel;

    public byte[] RowVersion { get; private set; } = [];

    public static StockItem Create(
        Guid productId,
        string sku,
        decimal initialQuantity,
        decimal minLevel,
        decimal maxLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(initialQuantity);

        var stockItem = new StockItem
        {
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            QuantityOnHand = initialQuantity,
        };
        stockItem.SetPolicy(minLevel, maxLevel);
        return stockItem;
    }

    public Result Adjust(decimal quantityChange)
    {
        if (quantityChange == 0)
        {
            return StockErrors.ZeroAdjustment;
        }

        if (QuantityOnHand + quantityChange < 0)
        {
            return StockErrors.InsufficientStock(ProductId, QuantityOnHand, quantityChange);
        }

        QuantityOnHand += quantityChange;
        return Result.Success();
    }

    public void SetPolicy(decimal minLevel, decimal maxLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLevel);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLevel);
        if (maxLevel < minLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLevel), "Max level must be >= min level.");
        }

        MinLevel = minLevel;
        MaxLevel = maxLevel;
    }
}
