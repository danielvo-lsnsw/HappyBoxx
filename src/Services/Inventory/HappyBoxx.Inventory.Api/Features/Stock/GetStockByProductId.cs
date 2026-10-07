using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.Inventory.Api.Domain;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Inventory.Api.Features.Stock;

internal sealed class GetStockByProductIdHandler(InventoryDbContext dbContext) : IHandler
{
    public async Task<Result<StockItemResponse>> HandleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var stockItem = await dbContext.StockItems
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(StockItemResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return stockItem is null ? StockErrors.NotFound(productId) : stockItem;
    }
}

internal sealed class GetStockByProductIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("stock/{productId:guid}", HandleAsync)
            .WithName("GetStockByProductId")
            .WithTags(Tags.Stock)
            .WithSummary("Get stock level for a product");

    private static async Task<Results<Ok<StockItemResponse>, ProblemHttpResult>> HandleAsync(
        Guid productId,
        GetStockByProductIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(productId, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
