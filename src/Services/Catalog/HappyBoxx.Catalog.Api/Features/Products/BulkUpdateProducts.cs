using System.Security.Claims;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record BulkUpdateProductsRequest(
    IReadOnlyList<Guid> ProductIds,
    bool? IsActive = null,
    Guid? CategoryId = null,
    Domain.Products.UnitOfMeasure? Unit = null,
    IReadOnlyList<string>? SupplierNames = null,
    string? DefaultSupplierName = null);

public sealed record BulkUpdateProductsResponse(int UpdatedCount);

internal sealed class BulkUpdateProductsValidator : AbstractValidator<BulkUpdateProductsRequest>
{
    public BulkUpdateProductsValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty();
        RuleForEach(x => x.ProductIds).NotEmpty();
        RuleForEach(x => x.SupplierNames).NotEmpty().MaximumLength(Domain.Suppliers.Supplier.NameMaxLength);
        RuleFor(x => x.DefaultSupplierName).MaximumLength(Domain.Suppliers.Supplier.NameMaxLength);

        RuleFor(x => x)
            .Must(x =>
                x.IsActive.HasValue ||
                x.CategoryId.HasValue ||
                x.Unit.HasValue ||
                x.SupplierNames is { Count: > 0 } ||
                !string.IsNullOrWhiteSpace(x.DefaultSupplierName))
            .WithMessage("At least one update field must be provided.");
    }
}

internal sealed class BulkUpdateProductsHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<BulkUpdateProductsResponse>> HandleAsync(
        BulkUpdateProductsRequest request,
        string changedBy,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId is { } categoryId &&
            !await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Domain.Products.ProductErrors.CategoryNotFound(categoryId);
        }

        var ids = request.ProductIds.Distinct().ToArray();
        var products = await dbContext.Products
            .Include(p => p.ProductSuppliers)
            .ThenInclude(ps => ps.Supplier)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            var before = ProductChangeHistoryRecorder.Capture(product);
            product.Update(
                product.LegacyCode,
                product.Name,
                product.Description,
                request.CategoryId ?? product.CategoryId,
                request.Unit ?? product.Unit,
                product.PackDetails,
                product.SellPriceNear,
                product.SellPriceFar,
                product.BuyPrice,
                product.PriceEffectiveDate,
                request.IsActive ?? product.IsActive);

            var incomingNames = request.SupplierNames is { Count: > 0 }
                ? request.SupplierNames
                : product.ProductSuppliers.Select(ps => ps.Supplier.Name).ToArray();
            var incomingDefault = !string.IsNullOrWhiteSpace(request.DefaultSupplierName)
                ? request.DefaultSupplierName
                : product.ProductSuppliers.FirstOrDefault(ps => ps.IsDefault)?.Supplier.Name;

            var links = await ProductSupplierHelpers.ResolveSupplierLinksAsync(
                dbContext,
                incomingNames,
                incomingDefault,
                cancellationToken);

            var defaultSupplier = links.FirstOrDefault(link => link.IsDefault);
            product.SetSuppliers(
                links.Select(link => link.SupplierId).ToArray(),
                defaultSupplier.SupplierId == Guid.Empty ? null : defaultSupplier.SupplierId);
            product.CaptureDefaultSupplierLastBuyPrice(changedAtUtc);
            ProductChangeHistoryRecorder.Record(dbContext, product, before, changedBy, changedAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new BulkUpdateProductsResponse(products.Count);
    }
}

internal sealed class BulkUpdateProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("products/bulk/update", HandleAsync)
            .WithName("BulkUpdateProducts")
            .WithTags(Tags.Products)
            .WithSummary("Bulk update product status/category/unit/default supplier")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<BulkUpdateProductsRequest>();

    private static async Task<Results<Ok<BulkUpdateProductsResponse>, ProblemHttpResult>> HandleAsync(
        BulkUpdateProductsRequest request,
        ClaimsPrincipal principal,
        TimeProvider timeProvider,
        BulkUpdateProductsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            request,
            ProductChangeHistoryRecorder.ResolveActor(principal),
            timeProvider.GetUtcNow(),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
