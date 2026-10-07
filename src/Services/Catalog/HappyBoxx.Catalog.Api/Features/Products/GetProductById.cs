using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

internal sealed class GetProductByIdHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ProductResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? ProductErrors.NotFound(id) : product;
    }
}

internal sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products/{id:guid}", HandleAsync)
            .WithName("GetProductById")
            .WithTags(Tags.Products)
            .WithSummary("Get a product by id");

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        GetProductByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
