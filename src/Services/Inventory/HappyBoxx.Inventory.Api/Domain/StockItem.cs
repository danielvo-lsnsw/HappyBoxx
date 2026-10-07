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

    public decimal ReorderLevel { get; private set; }

    public bool IsLowStock => QuantityOnHand <= ReorderLevel;

    public byte[] RowVersion { get; private set; } = [];

    public static StockItem Create(Guid productId, string sku, decimal initialQuantity, decimal reorderLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(initialQuantity);
        ArgumentOutOfRangeException.ThrowIfNegative(reorderLevel);

        return new StockItem
        {
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            QuantityOnHand = initialQuantity,
            ReorderLevel = reorderLevel,
        };
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

    public void SetReorderLevel(decimal reorderLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reorderLevel);
        ReorderLevel = reorderLevel;
    }
}
