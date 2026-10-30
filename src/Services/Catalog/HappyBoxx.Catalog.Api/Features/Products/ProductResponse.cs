using System.Linq.Expressions;
using HappyBoxx.Catalog.Api.Domain.Products;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record ProductSupplierResponse(
    Guid Id,
    string Name,
    bool IsDefault,
    decimal? LastBuyPrice,
    DateTimeOffset? LastBuyPriceAtUtc);

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string? LegacyCode,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    UnitOfMeasure Unit,
    string? PackDetails,
    decimal? SellPriceNear,
    decimal? SellPriceFar,
    decimal? BuyPrice,
    DateOnly? PriceEffectiveDate,
    IReadOnlyList<ProductSupplierResponse> Suppliers,
    decimal Price,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    internal static readonly Expression<Func<Product, ProductResponse>> Projection = p =>
        new ProductResponse(
            p.Id,
            p.Sku,
            p.LegacyCode,
            p.Name,
            p.Description,
            p.CategoryId,
            // Translated to a SQL join over a required FK, so never null here.
            p.Category!.Name,
            p.Unit,
            p.PackDetails,
            p.SellPriceNear,
            p.SellPriceFar,
            p.BuyPrice,
            p.PriceEffectiveDate,
            p.ProductSuppliers
                .OrderByDescending(ps => ps.IsDefault)
                .ThenBy(ps => ps.Supplier.Name)
                .Select(ps => new ProductSupplierResponse(
                    ps.SupplierId,
                    ps.Supplier.Name,
                    ps.IsDefault,
                    ps.LastBuyPrice,
                    ps.LastBuyPriceAtUtc))
                .ToList(),
            p.SellPriceNear ?? 0m,
            p.IsActive,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);
}
