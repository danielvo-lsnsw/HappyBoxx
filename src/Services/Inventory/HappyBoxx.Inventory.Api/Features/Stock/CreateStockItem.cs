using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Inventory.Api.Domain;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Inventory.Api.Features.Stock;

public sealed record CreateStockItemRequest(Guid ProductId, string Sku, decimal InitialQuantity, decimal ReorderLevel);

internal sealed class CreateStockItemValidator : AbstractValidator<CreateStockItemRequest>
{
    public CreateStockItemValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(StockItem.SkuMaxLength);
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
    }
}

internal sealed class CreateStockItemHandler(InventoryDbContext dbContext) : IHandler
{
    public async Task<Result<StockItemResponse>> HandleAsync(CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.StockItems.AnyAsync(s => s.ProductId == request.ProductId, cancellationToken))
        {
            return StockErrors.AlreadyExists(request.ProductId);
        }

        var stockItem = StockItem.Create(request.ProductId, request.Sku, request.InitialQuantity, request.ReorderLevel);
        dbContext.StockItems.Add(stockItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StockItemResponse.From(stockItem);
    }
}

internal sealed class CreateStockItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("stock", HandleAsync)
            .WithName("CreateStockItem")
            .WithTags(Tags.Stock)
            .WithSummary("Start tracking stock for a product")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<CreateStockItemRequest>();

    private static async Task<Results<CreatedAtRoute<StockItemResponse>, ProblemHttpResult>> HandleAsync(
        CreateStockItemRequest request,
        CreateStockItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStockByProductId", new { productId = result.Value.ProductId })
            : result.Error.ToProblem();
    }
}
