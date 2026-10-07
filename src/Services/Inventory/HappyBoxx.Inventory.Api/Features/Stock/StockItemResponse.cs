using System.Linq.Expressions;
using HappyBoxx.Inventory.Api.Domain;

namespace HappyBoxx.Inventory.Api.Features.Stock;

public sealed record StockItemResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsLowStock,
    DateTimeOffset UpdatedAtUtc)
{
    internal static readonly Expression<Func<StockItem, StockItemResponse>> Projection = s =>
        new StockItemResponse(
            s.Id,
            s.ProductId,
            s.Sku,
            s.QuantityOnHand,
            s.ReorderLevel,
            s.QuantityOnHand <= s.ReorderLevel,
            s.UpdatedAtUtc);

    private static readonly Func<StockItem, StockItemResponse> CompiledProjection = Projection.Compile();

    internal static StockItemResponse From(StockItem stockItem) => CompiledProjection(stockItem);
}

internal static class Tags
{
    public const string Stock = "Stock";
}
