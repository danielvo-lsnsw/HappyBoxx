using System.Security.Claims;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;

namespace HappyBoxx.Catalog.Api.Features.Products;

internal static class ProductChangeHistoryRecorder
{
    public sealed record Snapshot(
        string Name,
        Guid CategoryId,
        bool IsActive,
        decimal? SellPriceNear,
        decimal? SellPriceFar,
        decimal? BuyPrice,
        DateOnly? PriceEffectiveDate,
        string SupplierKeys,
        Guid? DefaultSupplierId);

    public static Snapshot Capture(Product product)
    {
        var supplierKeys = string.Join(
            ", ",
            product.ProductSuppliers
                .Select(x => x.SupplierId)
                .OrderBy(x => x)
                .Select(x => x.ToString()));
        var defaultSupplierId = product.ProductSuppliers
            .FirstOrDefault(x => x.IsDefault)?
            .SupplierId;

        return new Snapshot(
            product.Name,
            product.CategoryId,
            product.IsActive,
            product.SellPriceNear,
            product.SellPriceFar,
            product.BuyPrice,
            product.PriceEffectiveDate,
            supplierKeys,
            defaultSupplierId);
    }

    public static string ResolveActor(ClaimsPrincipal principal) =>
        principal.FindFirst("name")?.Value
        ?? principal.FindFirst("preferred_username")?.Value
        ?? principal.FindFirst("email")?.Value
        ?? principal.FindFirst("emails")?.Value
        ?? principal.FindFirst("sub")?.Value
        ?? "unknown";

    public static void Record(
        CatalogDbContext dbContext,
        Product product,
        Snapshot before,
        string changedBy,
        DateTimeOffset changedAtUtc)
    {
        var after = Capture(product);

        AddIfChanged(dbContext, product.Id, "Name", before.Name, after.Name, changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "CategoryId", before.CategoryId.ToString(), after.CategoryId.ToString(), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "IsActive", before.IsActive.ToString(), after.IsActive.ToString(), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "SellPriceNear", Format(before.SellPriceNear), Format(after.SellPriceNear), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "SellPriceFar", Format(before.SellPriceFar), Format(after.SellPriceFar), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "BuyPrice", Format(before.BuyPrice), Format(after.BuyPrice), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "PriceEffectiveDate", Format(before.PriceEffectiveDate), Format(after.PriceEffectiveDate), changedBy, changedAtUtc);
        AddIfChanged(dbContext, product.Id, "Suppliers", EmptyToNull(before.SupplierKeys), EmptyToNull(after.SupplierKeys), changedBy, changedAtUtc);
        AddIfChanged(
            dbContext,
            product.Id,
            "DefaultSupplier",
            before.DefaultSupplierId?.ToString(),
            after.DefaultSupplierId?.ToString(),
            changedBy,
            changedAtUtc);
    }

    private static void AddIfChanged(
        CatalogDbContext dbContext,
        Guid productId,
        string fieldName,
        string? before,
        string? after,
        string changedBy,
        DateTimeOffset changedAtUtc)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return;
        }

        dbContext.ProductChangeHistoryEntries.Add(ProductChangeHistoryEntry.Create(
            productId,
            fieldName,
            before,
            after,
            changedBy,
            changedAtUtc));
    }

    private static string? Format(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : null;

    private static string? Format(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
