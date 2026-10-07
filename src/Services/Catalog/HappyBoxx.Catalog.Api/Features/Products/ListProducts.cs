using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null) : IPagedQuery;

internal sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

internal sealed class ListProductsHandler(CatalogDbContext dbContext) : IHandler
{
    public Task<PagedResult<ProductResponse>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.Where(p => p.Name.Contains(query.Search) || p.Sku.Contains(query.Search));
        }

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        if (query.IsActive is { } isActive)
        {
            products = products.Where(p => p.IsActive == isActive);
        }

        return products
            .OrderBy(p => p.Name)
            .Select(ProductResponse.Projection)
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products", HandleAsync)
            .WithName("ListProducts")
            .WithTags(Tags.Products)
            .WithSummary("List products (paged, filterable)")
            .WithRequestValidation<ListProductsQuery>();

    private static async Task<Ok<PagedResult<ProductResponse>>> HandleAsync(
        [AsParameters] ListProductsQuery query,
        ListProductsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
