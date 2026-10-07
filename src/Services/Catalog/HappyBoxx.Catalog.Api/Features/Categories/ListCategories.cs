using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Categories;

public sealed record ListCategoriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IPagedQuery;

internal sealed class ListCategoriesValidator : AbstractValidator<ListCategoriesQuery>
{
    public ListCategoriesValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

internal sealed class ListCategoriesHandler(CatalogDbContext dbContext) : IHandler
{
    public Task<PagedResult<CategoryResponse>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            categories = categories.Where(c => c.Name.Contains(query.Search));
        }

        if (query.IsActive is { } isActive)
        {
            categories = categories.Where(c => c.IsActive == isActive);
        }

        return categories
            .OrderBy(c => c.Name)
            .Select(CategoryResponse.Projection)
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("categories", HandleAsync)
            .WithName("ListCategories")
            .WithTags(Tags.Categories)
            .WithSummary("List categories (paged)")
            .WithRequestValidation<ListCategoriesQuery>();

    private static async Task<Ok<PagedResult<CategoryResponse>>> HandleAsync(
        [AsParameters] ListCategoriesQuery query,
        ListCategoriesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
