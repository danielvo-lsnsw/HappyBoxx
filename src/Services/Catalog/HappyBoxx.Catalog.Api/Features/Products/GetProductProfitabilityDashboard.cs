using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record ProductProfitabilityQuery(int TrendDays = 30, decimal LowMarginThresholdPct = 10m);

public sealed record ProductProfitabilityItemResponse(
    Guid ProductId,
    string Sku,
    string Name,
    Guid CategoryId,
    string CategoryName,
    decimal? BuyPrice,
    decimal? SellPriceNear,
    decimal? SellPriceFar,
    decimal? NearMarginPct,
    decimal? FarMarginPct,
    decimal? NearMarginTrendPct,
    decimal? FarMarginTrendPct,
    bool IsLowMargin,
    bool IsNegativeMargin);

public sealed record CategoryProfitabilitySummaryResponse(
    Guid CategoryId,
    string CategoryName,
    decimal? AvgNearMarginPct,
    decimal? AvgFarMarginPct,
    int ProductCount,
    int LowMarginCount,
    int NegativeMarginCount);

public sealed record ProductProfitabilityDashboardResponse(
    DateTimeOffset GeneratedAtUtc,
    decimal LowMarginThresholdPct,
    IReadOnlyList<ProductProfitabilityItemResponse> Products,
    IReadOnlyList<CategoryProfitabilitySummaryResponse> Categories);

internal sealed class ProductProfitabilityQueryValidator : AbstractValidator<ProductProfitabilityQuery>
{
    public ProductProfitabilityQueryValidator()
    {
        RuleFor(x => x.TrendDays).InclusiveBetween(7, 365);
        RuleFor(x => x.LowMarginThresholdPct).InclusiveBetween(0, 100);
    }
}

internal sealed class GetProductProfitabilityDashboardHandler(CatalogDbContext dbContext, TimeProvider timeProvider) : IHandler
{
    public async Task<ProductProfitabilityDashboardResponse> HandleAsync(
        ProductProfitabilityQuery query,
        CancellationToken cancellationToken)
    {
        var from = timeProvider.GetUtcNow().AddDays(-query.TrendDays);

        var products = await dbContext.Products
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name,
                p.CategoryId,
                CategoryName = p.Category!.Name,
                p.BuyPrice,
                p.SellPriceNear,
                p.SellPriceFar,
            })
            .ToListAsync(cancellationToken);

        var relevantHistory = await dbContext.ProductChangeHistoryEntries
            .AsNoTracking()
            .Where(x => x.ChangedAtUtc >= from &&
                        (x.FieldName == "SellPriceNear" || x.FieldName == "SellPriceFar" || x.FieldName == "BuyPrice"))
            .OrderBy(x => x.ChangedAtUtc)
            .ToListAsync(cancellationToken);

        var items = products.Select(product =>
        {
            var productHistory = relevantHistory.Where(x => x.ProductId == product.Id).ToArray();
            var baseNear = ResolveBaseline(product.SellPriceNear, productHistory, "SellPriceNear");
            var baseFar = ResolveBaseline(product.SellPriceFar, productHistory, "SellPriceFar");
            var baseBuy = ResolveBaseline(product.BuyPrice, productHistory, "BuyPrice");

            var nearMarginPct = MarginPct(product.SellPriceNear, product.BuyPrice);
            var farMarginPct = MarginPct(product.SellPriceFar, product.BuyPrice);
            var baseNearMarginPct = MarginPct(baseNear, baseBuy);
            var baseFarMarginPct = MarginPct(baseFar, baseBuy);

            var lowMargin = IsLowMargin(nearMarginPct, farMarginPct, query.LowMarginThresholdPct);
            var negativeMargin = IsNegativeMargin(nearMarginPct, farMarginPct);

            return new ProductProfitabilityItemResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.CategoryId,
                product.CategoryName,
                product.BuyPrice,
                product.SellPriceNear,
                product.SellPriceFar,
                Round(nearMarginPct),
                Round(farMarginPct),
                Round(Diff(nearMarginPct, baseNearMarginPct)),
                Round(Diff(farMarginPct, baseFarMarginPct)),
                lowMargin,
                negativeMargin);
        }).OrderBy(x => x.CategoryName).ThenBy(x => x.Sku).ToArray();

        var categorySummaries = items
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(group =>
            {
                var nearValues = group.Where(x => x.NearMarginPct.HasValue).Select(x => x.NearMarginPct!.Value).ToArray();
                var farValues = group.Where(x => x.FarMarginPct.HasValue).Select(x => x.FarMarginPct!.Value).ToArray();
                return new CategoryProfitabilitySummaryResponse(
                    group.Key.CategoryId,
                    group.Key.CategoryName,
                    nearValues.Length == 0 ? null : Round(nearValues.Average()),
                    farValues.Length == 0 ? null : Round(farValues.Average()),
                    group.Count(),
                    group.Count(x => x.IsLowMargin),
                    group.Count(x => x.IsNegativeMargin));
            })
            .OrderBy(x => x.CategoryName)
            .ToArray();

        return new ProductProfitabilityDashboardResponse(
            timeProvider.GetUtcNow(),
            query.LowMarginThresholdPct,
            items,
            categorySummaries);
    }

    private static decimal? ResolveBaseline(decimal? current, IReadOnlyList<Domain.Products.ProductChangeHistoryEntry> history, string fieldName)
    {
        var first = history.FirstOrDefault(x => x.FieldName == fieldName);
        if (first is null)
        {
            return current;
        }

        return TryParseDecimal(first.OldValue) ?? current;
    }

    private static decimal? MarginPct(decimal? sell, decimal? buy)
    {
        if (!sell.HasValue || !buy.HasValue || sell.Value <= 0)
        {
            return null;
        }

        return ((sell.Value - buy.Value) / sell.Value) * 100m;
    }

    private static decimal? Diff(decimal? current, decimal? baseline)
    {
        if (!current.HasValue || !baseline.HasValue)
        {
            return null;
        }

        return current.Value - baseline.Value;
    }

    private static bool IsLowMargin(decimal? near, decimal? far, decimal thresholdPct)
    {
        if (near.HasValue && near.Value >= 0 && near.Value < thresholdPct)
        {
            return true;
        }

        return far.HasValue && far.Value >= 0 && far.Value < thresholdPct;
    }

    private static bool IsNegativeMargin(decimal? near, decimal? far) =>
        (near.HasValue && near.Value < 0) || (far.HasValue && far.Value < 0);

    private static decimal? Round(decimal? value) =>
        value.HasValue ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;

    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal? TryParseDecimal(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}

internal sealed class GetProductProfitabilityDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products/profitability", HandleAsync)
            .WithName("GetProductProfitabilityDashboard")
            .WithTags(Tags.Products)
            .WithSummary("Get product-level and category-level profitability insights")
            .WithRequestValidation<ProductProfitabilityQuery>();

    private static async Task<Ok<ProductProfitabilityDashboardResponse>> HandleAsync(
        [AsParameters] ProductProfitabilityQuery query,
        GetProductProfitabilityDashboardHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
