using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure Unit,
    decimal Price,
    bool IsActive);

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

internal sealed class UpdateProductHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return ProductErrors.CategoryNotFound(request.CategoryId);
        }

        product.Update(request.Name, request.Description, request.CategoryId, request.Unit, request.Price, request.IsActive);
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
            .WithRequestValidation<UpdateProductRequest>();

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        UpdateProductRequest request,
        UpdateProductHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
