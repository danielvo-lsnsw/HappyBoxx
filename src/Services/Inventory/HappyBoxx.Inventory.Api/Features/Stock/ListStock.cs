using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Inventory.Api.Features.Stock;

public sealed record ListStockQuery(
    int Page = 1,
    int PageSize = 20,
    string? Sku = null,
    bool LowStockOnly = false) : IPagedQuery;

internal sealed class ListStockValidator : AbstractValidator<ListStockQuery>
{
    public ListStockValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Sku).MaximumLength(50);
    }
}

internal sealed class ListStockHandler(InventoryDbContext dbContext) : IHandler
{
    public Task<PagedResult<StockItemResponse>> HandleAsync(ListStockQuery query, CancellationToken cancellationToken)
    {
        var stock = dbContext.StockItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Sku))
        {
            stock = stock.Where(s => s.Sku.Contains(query.Sku));
        }

        if (query.LowStockOnly)
        {
            stock = stock.Where(s => s.QuantityOnHand <= s.ReorderLevel);
        }

        return stock
            .OrderBy(s => s.Sku)
            .Select(StockItemResponse.Projection)
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("stock", HandleAsync)
            .WithName("ListStock")
            .WithTags(Tags.Stock)
            .WithSummary("List stock levels (paged)")
            .WithRequestValidation<ListStockQuery>();

    private static async Task<Ok<PagedResult<StockItemResponse>>> HandleAsync(
        [AsParameters] ListStockQuery query,
        ListStockHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
