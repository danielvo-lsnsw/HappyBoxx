using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record ProductChangeHistoryItemResponse(
    Guid Id,
    string FieldName,
    string? OldValue,
    string? NewValue,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

public sealed record ListProductChangeHistoryQuery(int Take = 100);

internal sealed class ListProductChangeHistoryValidator : AbstractValidator<ListProductChangeHistoryQuery>
{
    public ListProductChangeHistoryValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

internal sealed class ListProductChangeHistoryHandler(CatalogDbContext dbContext) : IHandler
{
    public Task<List<ProductChangeHistoryItemResponse>> HandleAsync(
        Guid productId,
        ListProductChangeHistoryQuery query,
        CancellationToken cancellationToken)
        => dbContext.ProductChangeHistoryEntries
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(query.Take)
            .Select(x => new ProductChangeHistoryItemResponse(
                x.Id,
                x.FieldName,
                x.OldValue,
                x.NewValue,
                x.ChangedBy,
                x.ChangedAtUtc))
            .ToListAsync(cancellationToken);
}

internal sealed class ListProductChangeHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products/{id:guid}/change-history", HandleAsync)
            .WithName("ListProductChangeHistory")
            .WithTags(Tags.Products)
            .WithSummary("List recent product change history")
            .WithRequestValidation<ListProductChangeHistoryQuery>();

    private static async Task<Ok<List<ProductChangeHistoryItemResponse>>> HandleAsync(
        Guid id,
        [AsParameters] ListProductChangeHistoryQuery query,
        ListProductChangeHistoryHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, query, cancellationToken));
}
