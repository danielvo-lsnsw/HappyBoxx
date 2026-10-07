using System.Linq.Expressions;
using HappyBoxx.Catalog.Api.Domain.Products;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    UnitOfMeasure Unit,
    decimal Price,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    internal static readonly Expression<Func<Product, ProductResponse>> Projection = p =>
        new ProductResponse(
            p.Id,
            p.Sku,
            p.Name,
            p.Description,
            p.CategoryId,
            // Translated to a SQL join over a required FK, so never null here.
            p.Category!.Name,
            p.Unit,
            p.Price,
            p.IsActive,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);
}
