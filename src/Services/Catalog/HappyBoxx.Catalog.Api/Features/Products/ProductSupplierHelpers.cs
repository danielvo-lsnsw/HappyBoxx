using HappyBoxx.Catalog.Api.Domain.Suppliers;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

internal static class ProductSupplierHelpers
{
    public static async Task<IReadOnlyList<(Guid SupplierId, bool IsDefault)>> ResolveSupplierLinksAsync(
        CatalogDbContext dbContext,
        IReadOnlyList<string>? supplierNames,
        string? defaultSupplierName,
        CancellationToken cancellationToken)
    {
        var normalizedNames = NormalizeNames(supplierNames);
        if (normalizedNames.Count == 0)
        {
            return [];
        }

        var map = await dbContext.Suppliers
            .Where(s => normalizedNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var name in normalizedNames.Where(name => !map.ContainsKey(name)))
        {
            var supplier = Supplier.Create(name);
            dbContext.Suppliers.Add(supplier);
            map[name] = supplier;
        }

        var resolvedDefault = string.IsNullOrWhiteSpace(defaultSupplierName)
            ? normalizedNames[0]
            : defaultSupplierName.Trim();

        if (!map.ContainsKey(resolvedDefault))
        {
            resolvedDefault = normalizedNames[0];
        }

        return normalizedNames
            .Select(name => (map[name].Id, string.Equals(name, resolvedDefault, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    public static List<string> NormalizeNames(IReadOnlyList<string>? supplierNames)
    {
        if (supplierNames is null)
        {
            return [];
        }

        var normalized = new List<string>();
        foreach (var supplierName in supplierNames)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                continue;
            }

            var value = supplierName.Trim();
            if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }
}
