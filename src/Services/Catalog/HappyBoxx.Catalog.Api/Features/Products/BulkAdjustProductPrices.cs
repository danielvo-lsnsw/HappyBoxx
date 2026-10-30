using System.Security.Claims;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public enum BulkPriceAdjustmentMode
{
    Percentage = 1,
    Absolute = 2,
}

public sealed record BulkAdjustProductPricesRequest(
    IReadOnlyList<Guid> ProductIds,
    BulkPriceAdjustmentMode Mode,
    decimal? NearChange = null,
    decimal? FarChange = null);

public sealed record BulkAdjustProductPricesResponse(int UpdatedCount);

internal sealed class BulkAdjustProductPricesValidator : AbstractValidator<BulkAdjustProductPricesRequest>
{
    public BulkAdjustProductPricesValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty();
        RuleForEach(x => x.ProductIds).NotEmpty();
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x)
            .Must(x => x.NearChange.HasValue || x.FarChange.HasValue)
            .WithMessage("NearChange or FarChange must be provided.");
    }
}

internal sealed class BulkAdjustProductPricesHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<BulkAdjustProductPricesResponse> HandleAsync(
        BulkAdjustProductPricesRequest request,
        string changedBy,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        var ids = request.ProductIds.Distinct().ToArray();
        var products = await dbContext.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            var before = ProductChangeHistoryRecorder.Capture(product);
            var nearPrice = Apply(product.SellPriceNear, request.Mode, request.NearChange);
            var farPrice = Apply(product.SellPriceFar, request.Mode, request.FarChange);

            product.Update(
                product.LegacyCode,
                product.Name,
                product.Description,
                product.CategoryId,
                product.Unit,
                product.PackDetails,
                nearPrice,
                farPrice,
                product.BuyPrice,
                product.PriceEffectiveDate,
                product.IsActive);
            ProductChangeHistoryRecorder.Record(dbContext, product, before, changedBy, changedAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new BulkAdjustProductPricesResponse(products.Count);
    }

    private static decimal? Apply(decimal? current, BulkPriceAdjustmentMode mode, decimal? delta)
    {
        if (!delta.HasValue || !current.HasValue)
        {
            return current;
        }

        var nextValue = mode switch
        {
            BulkPriceAdjustmentMode.Percentage => current.Value * (1 + (delta.Value / 100m)),
            BulkPriceAdjustmentMode.Absolute => current.Value + delta.Value,
            _ => current.Value,
        };

        if (nextValue < 0)
        {
            nextValue = 0;
        }

        return decimal.Round(nextValue, 3, MidpointRounding.AwayFromZero);
    }
}

internal sealed class BulkAdjustProductPricesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("products/bulk/price-adjust", HandleAsync)
            .WithName("BulkAdjustProductPrices")
            .WithTags(Tags.Products)
            .WithSummary("Bulk adjust near/far prices by percentage or absolute delta")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<BulkAdjustProductPricesRequest>();

    private static async Task<Ok<BulkAdjustProductPricesResponse>> HandleAsync(
        BulkAdjustProductPricesRequest request,
        ClaimsPrincipal principal,
        TimeProvider timeProvider,
        BulkAdjustProductPricesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(
            request,
            ProductChangeHistoryRecorder.ResolveActor(principal),
            timeProvider.GetUtcNow(),
            cancellationToken));
}
