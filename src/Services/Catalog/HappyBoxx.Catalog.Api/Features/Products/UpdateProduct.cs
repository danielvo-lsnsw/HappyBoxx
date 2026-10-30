using System.Security.Claims;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Domain.Suppliers;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record UpdateProductRequest(
    string? LegacyCode,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure Unit,
    string? PackDetails,
    decimal? SellPriceNear,
    decimal? SellPriceFar,
    decimal? BuyPrice,
    DateOnly? PriceEffectiveDate,
    IReadOnlyList<string>? SupplierNames,
    string? DefaultSupplierName,
    bool IsActive);

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.LegacyCode).MaximumLength(Product.LegacyCodeMaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.PackDetails).MaximumLength(Product.PackDetailsMaxLength);
        RuleFor(x => x.SellPriceNear)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 3, ignoreTrailingZeros: true)
            .When(x => x.SellPriceNear.HasValue);
        RuleFor(x => x.SellPriceFar)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 3, ignoreTrailingZeros: true)
            .When(x => x.SellPriceFar.HasValue);
        RuleFor(x => x.BuyPrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 3, ignoreTrailingZeros: true)
            .When(x => x.BuyPrice.HasValue);
        RuleForEach(x => x.SupplierNames).NotEmpty().MaximumLength(Supplier.NameMaxLength);
        RuleFor(x => x.DefaultSupplierName).MaximumLength(Supplier.NameMaxLength);
    }
}

internal sealed class UpdateProductHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<ProductResponse>> HandleAsync(
        Guid id,
        UpdateProductRequest request,
        string changedBy,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductSuppliers)
            .ThenInclude(ps => ps.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return ProductErrors.CategoryNotFound(request.CategoryId);
        }

        var before = ProductChangeHistoryRecorder.Capture(product);
        var supplierLinks = await ProductSupplierHelpers.ResolveSupplierLinksAsync(
            dbContext,
            request.SupplierNames,
            request.DefaultSupplierName,
            cancellationToken);

        product.Update(
            request.LegacyCode,
            request.Name,
            request.Description,
            request.CategoryId,
            request.Unit,
            request.PackDetails,
            request.SellPriceNear,
            request.SellPriceFar,
            request.BuyPrice,
            request.PriceEffectiveDate,
            request.IsActive);
        var defaultSupplier = supplierLinks.FirstOrDefault(s => s.IsDefault);
        product.SetSuppliers(
            supplierLinks.Select(s => s.SupplierId).ToArray(),
            defaultSupplier.SupplierId == Guid.Empty ? null : defaultSupplier.SupplierId);
        product.CaptureDefaultSupplierLastBuyPrice(changedAtUtc);
        ProductChangeHistoryRecorder.Record(dbContext, product, before, changedBy, changedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ProductResponse.Projection)
            .FirstAsync(cancellationToken);
    }

}

internal sealed class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("products/{id:guid}", HandleAsync)
            .WithName("UpdateProduct")
            .WithTags(Tags.Products)
            .WithSummary("Update a product")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<UpdateProductRequest>();

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        UpdateProductRequest request,
        ClaimsPrincipal principal,
        TimeProvider timeProvider,
        UpdateProductHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            id,
            request,
            ProductChangeHistoryRecorder.ResolveActor(principal),
            timeProvider.GetUtcNow(),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
