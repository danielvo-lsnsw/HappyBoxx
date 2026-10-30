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
    bool? IsActive = null,
    string? SortBy = null,
    string? SortDir = null) : IPagedQuery;

internal sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy)
            .Must(BeValidSortBy)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("SortBy is not supported.");
        RuleFor(x => x.SortDir)
            .Must(x => x is "asc" or "desc")
            .When(x => !string.IsNullOrWhiteSpace(x.SortDir))
            .WithMessage("SortDir must be 'asc' or 'desc'.");
    }

    private static bool BeValidSortBy(string? sortBy) => sortBy is
        "sku" or
        "name" or
        "category" or
        "unit" or
        "near" or
        "far" or
        "status";
}

internal sealed class ListProductsHandler(CatalogDbContext dbContext) : IHandler
{
    public Task<PagedResult<ProductResponse>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.Where(p =>
                p.Name.Contains(query.Search) ||
                p.Sku.Contains(query.Search) ||
                (p.LegacyCode != null && p.LegacyCode.Contains(query.Search)));
        }

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        if (query.IsActive is { } isActive)
        {
            products = products.Where(p => p.IsActive == isActive);
        }

        var sortBy = query.SortBy?.Trim().ToLowerInvariant() ?? "sku";
        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        products = sortBy switch
        {
            "name" => descending ? products.OrderByDescending(p => p.Name).ThenBy(p => p.Sku) : products.OrderBy(p => p.Name).ThenBy(p => p.Sku),
            "category" => descending ? products.OrderByDescending(p => p.Category!.Name).ThenBy(p => p.Sku) : products.OrderBy(p => p.Category!.Name).ThenBy(p => p.Sku),
            "unit" => descending ? products.OrderByDescending(p => p.Unit).ThenBy(p => p.Sku) : products.OrderBy(p => p.Unit).ThenBy(p => p.Sku),
            "near" => descending ? products.OrderByDescending(p => p.SellPriceNear).ThenBy(p => p.Sku) : products.OrderBy(p => p.SellPriceNear).ThenBy(p => p.Sku),
            "far" => descending ? products.OrderByDescending(p => p.SellPriceFar).ThenBy(p => p.Sku) : products.OrderBy(p => p.SellPriceFar).ThenBy(p => p.Sku),
            "status" => descending ? products.OrderByDescending(p => p.IsActive).ThenBy(p => p.Sku) : products.OrderBy(p => p.IsActive).ThenBy(p => p.Sku),
            _ => descending ? products.OrderByDescending(p => p.Sku) : products.OrderBy(p => p.Sku),
        };

        return products.Select(ProductResponse.Projection).ToPagedResultAsync(query, cancellationToken);
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
