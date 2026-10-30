using System.Text.Json;
using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence;

internal static class CatalogSeeder
{
    private static readonly string[] LegacyCategoryNames =
    [
        "Vegetables",
        "Fruits",
        "Food Containers",
        "Containers",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var db = (CatalogDbContext)context;
        await RemoveLegacyCategoriesAsync(db, cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var seed = await LoadSeedAsync(cancellationToken);

        var categories = seed.Categories
            .Select(c =>
            {
                var category = Category.Create(c.Code, c.Name, c.Description);
                if (!c.IsActive)
                {
                    category.Update(c.Code, c.Name, c.Description, false);
                }

                return category;
            })
            .ToArray();

        db.Categories.AddRange(categories);

        var categoryMap = categories.ToDictionary(c => c.Code, c => c, StringComparer.OrdinalIgnoreCase);

        var supplierNames = seed.Products
            .SelectMany(p => p.SupplierNames ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var suppliers = supplierNames
            .Select(Supplier.Create)
            .ToArray();
        db.Suppliers.AddRange(suppliers);

        var supplierMap = suppliers.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var row in seed.Products)
        {
            if (!categoryMap.TryGetValue(row.CategoryCode, out var category))
            {
                continue;
            }

            var product = Product.Create(
                row.Code,
                row.LegacyCode,
                row.Name,
                row.Description,
                category.Id,
                Enum.TryParse<UnitOfMeasure>(row.Unit, ignoreCase: true, out var unit) ? unit : UnitOfMeasure.Piece,
                row.PackDetails,
                row.SellPriceNear,
                row.SellPriceFar,
                row.BuyPrice,
                row.PriceEffectiveDate);

            if (!row.IsActive)
            {
                product.Update(
                    row.LegacyCode,
                    row.Name,
                    row.Description,
                    category.Id,
                    Enum.TryParse<UnitOfMeasure>(row.Unit, ignoreCase: true, out var parsedUnit) ? parsedUnit : UnitOfMeasure.Piece,
                    row.PackDetails,
                    row.SellPriceNear,
                    row.SellPriceFar,
                    row.BuyPrice,
                    row.PriceEffectiveDate,
                    false);
            }

            var supplierIds = (row.SupplierNames ?? [])
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => supplierMap.TryGetValue(name, out var supplier) ? supplier.Id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToArray();

            Guid? defaultSupplierId = null;
            if (!string.IsNullOrWhiteSpace(row.DefaultSupplierName) &&
                supplierMap.TryGetValue(row.DefaultSupplierName.Trim(), out var defaultSupplier))
            {
                defaultSupplierId = defaultSupplier.Id;
            }
            else if (supplierIds.Length > 0)
            {
                defaultSupplierId = supplierIds[0];
            }

            product.SetSuppliers(supplierIds, defaultSupplierId);
            db.Products.Add(product);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveLegacyCategoriesAsync(CatalogDbContext db, CancellationToken cancellationToken)
    {
        var legacyCategories = await db.Categories
            .Where(c => LegacyCategoryNames.Contains(c.Name))
            .ToListAsync(cancellationToken);

        if (legacyCategories.Count == 0)
        {
            return;
        }

        var legacyCategoryIds = legacyCategories.Select(c => c.Id).ToArray();
        var legacyProducts = await db.Products
            .Where(p => legacyCategoryIds.Contains(p.CategoryId))
            .ToListAsync(cancellationToken);

        if (legacyProducts.Count > 0)
        {
            db.Products.RemoveRange(legacyProducts);
        }

        db.Categories.RemoveRange(legacyCategories);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<CatalogSeedData> LoadSeedAsync(CancellationToken cancellationToken)
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedData", "price-list-import.json"),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Infrastructure",
                "Persistence",
                "SeedData",
                "price-list-import.json")),
        };

        var seedPath = candidatePaths.FirstOrDefault(File.Exists);
        if (seedPath is null)
        {
            throw new InvalidOperationException(
                $"Seed file not found. Checked: {string.Join(", ", candidatePaths.Select(p => $"'{p}'"))}.");
        }

        await using var stream = File.OpenRead(seedPath);
        var data = await JsonSerializer.DeserializeAsync<CatalogSeedData>(stream, JsonOptions, cancellationToken);
        return data ?? throw new InvalidOperationException("Failed to deserialize product seed data.");
    }

    private sealed record CatalogSeedData(IReadOnlyList<CategorySeedRow> Categories, IReadOnlyList<ProductSeedRow> Products);

    private sealed record CategorySeedRow(string Code, string Prefix, string Name, string? Description, bool IsActive);

    private sealed record ProductSeedRow(
        string Code,
        string? LegacyCode,
        string Name,
        string? Description,
        string CategoryCode,
        string Unit,
        string? PackDetails,
        decimal? SellPriceNear,
        decimal? SellPriceFar,
        decimal? BuyPrice,
        DateOnly? PriceEffectiveDate,
        IReadOnlyList<string>? SupplierNames,
        string? DefaultSupplierName,
        bool IsActive);
}
