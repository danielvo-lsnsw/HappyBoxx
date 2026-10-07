using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Inventory.Api.Domain;

public static class StockErrors
{
    public static readonly Error ZeroAdjustment =
        Error.Validation("Stock.ZeroAdjustment", "Quantity change must not be zero.");

    public static Error NotFound(Guid productId) =>
        Error.NotFound("Stock.NotFound", $"No stock item exists for product '{productId}'.");

    public static Error AlreadyExists(Guid productId) =>
        Error.Conflict("Stock.AlreadyExists", $"A stock item already exists for product '{productId}'.");

    public static Error InsufficientStock(Guid productId, decimal onHand, decimal change) =>
        Error.Conflict(
            "Stock.Insufficient",
            $"Cannot apply change of {change} to product '{productId}': only {onHand} on hand.");
}
