using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Categories;

internal sealed class GetCategoryByIdHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<CategoryResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CategoryResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return category is null ? CategoryErrors.NotFound(id) : category;
    }
}

internal sealed class GetCategoryByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("categories/{id:guid}", HandleAsync)
            .WithName("GetCategoryById")
            .WithTags(Tags.Categories)
            .WithSummary("Get a category by id");

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        GetCategoryByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
