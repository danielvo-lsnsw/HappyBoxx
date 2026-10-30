using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Inventory.Api.Features.Stock;

public sealed record ListReorderSuggestionsQuery(int LookbackDays = 30, bool NeedThisWeekOnly = false);

public sealed record ReorderSuggestionResponse(
    Guid ProductId,
    string Sku,
    decimal QuantityOnHand,
    decimal MinLevel,
    decimal MaxLevel,
    decimal AvgDailyDemand,
    decimal ReorderPoint,
    decimal SuggestedOrderQuantity,
    bool NeedToOrderThisWeek);

internal sealed class ListReorderSuggestionsValidator : AbstractValidator<ListReorderSuggestionsQuery>
{
    public ListReorderSuggestionsValidator()
    {
        RuleFor(x => x.LookbackDays).InclusiveBetween(7, 180);
    }
}

internal sealed class ListReorderSuggestionsHandler(InventoryDbContext dbContext, TimeProvider timeProvider) : IHandler
{
    public async Task<IReadOnlyList<ReorderSuggestionResponse>> HandleAsync(
        ListReorderSuggestionsQuery query,
        CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow().AddDays(-query.LookbackDays);
        var demandByProduct = await dbContext.StockAdjustments
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= since && x.QuantityChange < 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Demand = -g.Sum(x => x.QuantityChange) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Demand, cancellationToken);

        var stockItems = await dbContext.StockItems
            .AsNoTracking()
            .OrderBy(x => x.Sku)
            .ToListAsync(cancellationToken);

        var suggestions = stockItems
            .Select(item =>
            {
                var demand = demandByProduct.TryGetValue(item.ProductId, out var totalDemand) ? totalDemand : 0m;
                var avgDailyDemand = query.LookbackDays <= 0 ? 0m : demand / query.LookbackDays;
                var demandForWeek = avgDailyDemand * 7m;
                var reorderPoint = Math.Max(item.MinLevel, demandForWeek);
                var suggested = item.QuantityOnHand <= reorderPoint
                    ? Math.Max(0m, item.MaxLevel - item.QuantityOnHand)
                    : 0m;
                suggested = decimal.Round(suggested, 3, MidpointRounding.AwayFromZero);
                avgDailyDemand = decimal.Round(avgDailyDemand, 3, MidpointRounding.AwayFromZero);
                reorderPoint = decimal.Round(reorderPoint, 3, MidpointRounding.AwayFromZero);
                var needThisWeek = item.QuantityOnHand <= decimal.Round(avgDailyDemand * 7m, 3, MidpointRounding.AwayFromZero)
                                   || suggested > 0m;

                return new ReorderSuggestionResponse(
                    item.ProductId,
                    item.Sku,
                    item.QuantityOnHand,
                    item.MinLevel,
                    item.MaxLevel,
                    avgDailyDemand,
                    reorderPoint,
                    suggested,
                    needThisWeek);
            })
            .Where(x => !query.NeedThisWeekOnly || x.NeedToOrderThisWeek)
            .OrderByDescending(x => x.NeedToOrderThisWeek)
            .ThenByDescending(x => x.SuggestedOrderQuantity)
            .ThenBy(x => x.Sku)
            .ToArray();

        return suggestions;
    }
}

internal sealed class ListReorderSuggestionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("stock/reorder-suggestions", HandleAsync)
            .WithName("ListReorderSuggestions")
            .WithTags(Tags.Stock)
            .WithSummary("List reorder suggestions based on demand velocity")
            .WithRequestValidation<ListReorderSuggestionsQuery>();

    private static async Task<Ok<IReadOnlyList<ReorderSuggestionResponse>>> HandleAsync(
        [AsParameters] ListReorderSuggestionsQuery query,
        ListReorderSuggestionsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
