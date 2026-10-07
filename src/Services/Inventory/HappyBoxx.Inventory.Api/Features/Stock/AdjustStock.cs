using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Inventory.Api.Domain;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Inventory.Api.Features.Stock;

/// <param name="QuantityChange">Positive to receive goods, negative to remove (damage, dispatch, correction).</param>
public sealed record AdjustStockRequest(decimal QuantityChange);

internal sealed class AdjustStockValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.QuantityChange).NotEqual(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
    }
}

internal sealed partial class AdjustStockHandler(InventoryDbContext dbContext, ILogger<AdjustStockHandler> logger) : IHandler
{
    public async Task<Result<StockItemResponse>> HandleAsync(Guid productId, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var stockItem = await dbContext.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        if (stockItem is null)
        {
            return StockErrors.NotFound(productId);
        }

        var result = stockItem.Adjust(request.QuantityChange);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogStockAdjusted(logger, productId, request.QuantityChange, stockItem.QuantityOnHand);

        return StockItemResponse.From(stockItem);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stock for product {ProductId} adjusted by {QuantityChange}; now {QuantityOnHand}")]
    private static partial void LogStockAdjusted(ILogger logger, Guid productId, decimal quantityChange, decimal quantityOnHand);
}

internal sealed class AdjustStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("stock/{productId:guid}/adjustments", HandleAsync)
            .WithName("AdjustStock")
            .WithTags(Tags.Stock)
            .WithSummary("Adjust the stock level of a product")
            .WithRequestValidation<AdjustStockRequest>();

    private static async Task<Results<Ok<StockItemResponse>, ProblemHttpResult>> HandleAsync(
        Guid productId,
        AdjustStockRequest request,
        AdjustStockHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(productId, request, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
