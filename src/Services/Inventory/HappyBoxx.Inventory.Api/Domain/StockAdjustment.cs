using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Inventory.Api.Domain;

public sealed class StockAdjustment : Entity
{
    private StockAdjustment()
    {
    }

    public Guid ProductId { get; private set; }

    public decimal QuantityChange { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static StockAdjustment Create(Guid productId, decimal quantityChange, DateTimeOffset occurredAtUtc) =>
        new()
        {
            ProductId = productId,
            QuantityChange = quantityChange,
            OccurredAtUtc = occurredAtUtc,
        };
}
