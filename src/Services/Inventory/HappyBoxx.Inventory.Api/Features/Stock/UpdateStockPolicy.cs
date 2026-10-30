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

public sealed record UpdateStockPolicyRequest(decimal MinLevel, decimal MaxLevel);

internal sealed class UpdateStockPolicyValidator : AbstractValidator<UpdateStockPolicyRequest>
{
    public UpdateStockPolicyValidator()
    {
        RuleFor(x => x.MinLevel).GreaterThanOrEqualTo(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x.MaxLevel).GreaterThanOrEqualTo(x => x.MinLevel).PrecisionScale(18, 3, ignoreTrailingZeros: true);
    }
}

internal sealed class UpdateStockPolicyHandler(InventoryDbContext dbContext) : IHandler
{
    public async Task<Result<StockItemResponse>> HandleAsync(
        Guid productId,
        UpdateStockPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var stockItem = await dbContext.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        if (stockItem is null)
        {
            return StockErrors.NotFound(productId);
        }

        stockItem.SetPolicy(request.MinLevel, request.MaxLevel);
        await dbContext.SaveChangesAsync(cancellationToken);
        return StockItemResponse.From(stockItem);
    }
}

internal sealed class UpdateStockPolicyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("stock/{productId:guid}/policy", HandleAsync)
            .WithName("UpdateStockPolicy")
            .WithTags(Tags.Stock)
            .WithSummary("Update min/max levels for a product")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<UpdateStockPolicyRequest>();

    private static async Task<Results<Ok<StockItemResponse>, ProblemHttpResult>> HandleAsync(
        Guid productId,
        UpdateStockPolicyRequest request,
        UpdateStockPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(productId, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
