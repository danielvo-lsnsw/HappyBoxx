using System.Linq.Expressions;
using HappyBoxx.Catalog.Api.Domain.Categories;

namespace HappyBoxx.Catalog.Api.Features.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    internal static readonly Expression<Func<Category, CategoryResponse>> Projection = c =>
        new CategoryResponse(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc);

    private static readonly Func<Category, CategoryResponse> CompiledProjection = Projection.Compile();

    internal static CategoryResponse From(Category category) => CompiledProjection(category);
}
