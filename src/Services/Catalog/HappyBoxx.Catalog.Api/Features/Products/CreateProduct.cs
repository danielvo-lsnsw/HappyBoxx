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

public sealed record CreateProductRequest(
    string Sku,
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
    string? DefaultSupplierName);

internal sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(Product.SkuMaxLength)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU may only contain letters, digits and dashes.");
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

internal sealed class CreateProductHandler(CatalogDbContext dbContext, TimeProvider timeProvider) : IHandler
{
    public async Task<Result<ProductResponse>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(sku);
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return ProductErrors.CategoryNotFound(request.CategoryId);
        }

        var supplierLinks = await ProductSupplierHelpers.ResolveSupplierLinksAsync(
            dbContext,
            request.SupplierNames,
            request.DefaultSupplierName,
            cancellationToken);

        var product = Product.Create(
            sku,
            request.LegacyCode,
            request.Name,
            request.Description,
            request.CategoryId,
            request.Unit,
            request.PackDetails,
            request.SellPriceNear,
            request.SellPriceFar,
            request.BuyPrice,
            request.PriceEffectiveDate);

        var defaultSupplier = supplierLinks.FirstOrDefault(s => s.IsDefault);
        product.SetSuppliers(
            supplierLinks.Select(s => s.SupplierId).ToArray(),
            defaultSupplier.SupplierId == Guid.Empty ? null : defaultSupplier.SupplierId);
        product.CaptureDefaultSupplierLastBuyPrice(timeProvider.GetUtcNow());

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == product.Id)
            .Select(ProductResponse.Projection)
            .FirstAsync(cancellationToken);
    }

}

internal sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("products", HandleAsync)
            .WithName("CreateProduct")
            .WithTags(Tags.Products)
            .WithSummary("Create a product")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<CreateProductRequest>();

    private static async Task<Results<CreatedAtRoute<ProductResponse>, ProblemHttpResult>> HandleAsync(
        CreateProductRequest request,
        CreateProductHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetProductById", new { id = result.Value.Id })
            : result.Error.ToProblem();
    }
}
