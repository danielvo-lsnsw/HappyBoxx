using System.Globalization;
using System.Security.Claims;
using System.Text;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Products;

public sealed record CsvImportError(int RowNumber, string Message);

public sealed record UpsertProductsFromCsvResponse(
    int CreatedCount,
    int UpdatedCount,
    int ErrorCount,
    IReadOnlyList<CsvImportError> Errors);

internal sealed class UpsertProductsFromCsvEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("products/import/csv", HandleAsync)
            .WithName("UpsertProductsFromCsv")
            .WithTags(Tags.Products)
            .WithSummary("Upsert products from CSV (SKU-based)")
            .RequireAuthorization(HappyBoxxPolicies.Admin);

    private static async Task<Results<Ok<UpsertProductsFromCsvResponse>, BadRequest<string>>> HandleAsync(
        IFormFile? file,
        ClaimsPrincipal principal,
        TimeProvider timeProvider,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.BadRequest("CSV file is required.");
        }

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return TypedResults.BadRequest("CSV header row is required.");
        }

        var headers = ParseCsvLine(headerLine)
            .Select((h, i) => new { Name = NormalizeHeader(h), Index = i })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

        if (!headers.ContainsKey("sku"))
        {
            return TypedResults.BadRequest("CSV must include an SKU column.");
        }

        var categories = await dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesByCode = categories.ToDictionary(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var categoriesByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var existingProducts = await dbContext.Products
            .Include(p => p.ProductSuppliers)
            .ThenInclude(ps => ps.Supplier)
            .ToListAsync(cancellationToken);
        var productsBySku = existingProducts.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        var rowNumber = 1;
        var created = 0;
        var updated = 0;
        var errors = new List<CsvImportError>();
        var changedBy = ProductChangeHistoryRecorder.ResolveActor(principal);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);
            var sku = Read(values, headers, "sku")?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(sku))
            {
                errors.Add(new CsvImportError(rowNumber, "SKU is required."));
                continue;
            }

            try
            {
                productsBySku.TryGetValue(sku, out var existing);
                var categoryId = ResolveCategoryId(values, headers, categoriesByCode, categoriesByName, existing?.CategoryId);
                if (!categoryId.HasValue)
                {
                    errors.Add(new CsvImportError(rowNumber, "Category could not be resolved (categoryCode/categoryName)."));
                    continue;
                }

                if (!TryParseUnit(Read(values, headers, "unit"), out var unit))
                {
                    unit = existing?.Unit ?? UnitOfMeasure.Piece;
                }

                if (!TryParseDecimal(Read(values, headers, "sellpricenear"), out var sellNear))
                {
                    TryParseDecimal(Read(values, headers, "near"), out sellNear);
                }

                if (!TryParseDecimal(Read(values, headers, "sellpricefar"), out var sellFar))
                {
                    TryParseDecimal(Read(values, headers, "far"), out sellFar);
                }

                if (!TryParseDecimal(Read(values, headers, "buyprice"), out var buyPrice))
                {
                    TryParseDecimal(Read(values, headers, "buy"), out buyPrice);
                }

                if (!TryParseDate(Read(values, headers, "priceeffectivedate"), out var effectiveDate))
                {
                    effectiveDate = existing?.PriceEffectiveDate;
                }

                var supplierNames = SplitSuppliers(Read(values, headers, "suppliers"));
                var defaultSupplierName = Read(values, headers, "defaultsuppliername")
                    ?? Read(values, headers, "defaultsupplier");
                if (!string.IsNullOrWhiteSpace(defaultSupplierName) &&
                    !supplierNames.Contains(defaultSupplierName.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    supplierNames.Add(defaultSupplierName.Trim());
                }

                var isActive = existing?.IsActive ?? true;
                if (TryParseBool(Read(values, headers, "isactive"), out var parsedActive))
                {
                    isActive = parsedActive;
                }

                if (existing is null)
                {
                    var name = Read(values, headers, "name")?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add(new CsvImportError(rowNumber, "Name is required for new products."));
                        continue;
                    }

                    var product = Product.Create(
                        sku,
                        Read(values, headers, "legacycode"),
                        name,
                        Read(values, headers, "description"),
                        categoryId.Value,
                        unit,
                        Read(values, headers, "packdetails"),
                        sellNear,
                        sellFar,
                        buyPrice,
                        effectiveDate);

                    if (!isActive)
                    {
                        product.Update(
                            product.LegacyCode,
                            product.Name,
                            product.Description,
                            product.CategoryId,
                            product.Unit,
                            product.PackDetails,
                            product.SellPriceNear,
                            product.SellPriceFar,
                            product.BuyPrice,
                            product.PriceEffectiveDate,
                            false);
                    }

                    var links = await ProductSupplierHelpers.ResolveSupplierLinksAsync(
                        dbContext,
                        supplierNames,
                        defaultSupplierName,
                        cancellationToken);
                    var defaultLink = links.FirstOrDefault(x => x.IsDefault);
                    product.SetSuppliers(
                        links.Select(x => x.SupplierId).ToArray(),
                        defaultLink.SupplierId == Guid.Empty ? null : defaultLink.SupplierId);
                    product.CaptureDefaultSupplierLastBuyPrice(timeProvider.GetUtcNow());

                    dbContext.Products.Add(product);
                    productsBySku[sku] = product;
                    created++;
                }
                else
                {
                    var before = ProductChangeHistoryRecorder.Capture(existing);
                    var name = Read(values, headers, "name")?.Trim();
                    var description = Read(values, headers, "description");
                    var legacyCode = Read(values, headers, "legacycode");
                    var packDetails = Read(values, headers, "packdetails");

                    existing.Update(
                        headers.ContainsKey("legacycode") ? legacyCode : existing.LegacyCode,
                        !string.IsNullOrWhiteSpace(name) ? name : existing.Name,
                        headers.ContainsKey("description") ? description : existing.Description,
                        categoryId.Value,
                        unit,
                        headers.ContainsKey("packdetails") ? packDetails : existing.PackDetails,
                        headers.ContainsKey("sellpricenear") || headers.ContainsKey("near") ? sellNear : existing.SellPriceNear,
                        headers.ContainsKey("sellpricefar") || headers.ContainsKey("far") ? sellFar : existing.SellPriceFar,
                        headers.ContainsKey("buyprice") || headers.ContainsKey("buy") ? buyPrice : existing.BuyPrice,
                        effectiveDate,
                        isActive);

                    if (supplierNames.Count > 0 || !string.IsNullOrWhiteSpace(defaultSupplierName))
                    {
                        var names = supplierNames.Count > 0
                            ? supplierNames
                            : existing.ProductSuppliers.Select(ps => ps.Supplier.Name).ToList();
                        var defaultName = !string.IsNullOrWhiteSpace(defaultSupplierName)
                            ? defaultSupplierName
                            : existing.ProductSuppliers.FirstOrDefault(ps => ps.IsDefault)?.Supplier.Name;
                        var links = await ProductSupplierHelpers.ResolveSupplierLinksAsync(
                            dbContext,
                            names,
                            defaultName,
                            cancellationToken);
                        var defaultLink = links.FirstOrDefault(x => x.IsDefault);
                        existing.SetSuppliers(
                            links.Select(x => x.SupplierId).ToArray(),
                            defaultLink.SupplierId == Guid.Empty ? null : defaultLink.SupplierId);
                    }

                    existing.CaptureDefaultSupplierLastBuyPrice(timeProvider.GetUtcNow());
                    ProductChangeHistoryRecorder.Record(
                        dbContext,
                        existing,
                        before,
                        changedBy,
                        timeProvider.GetUtcNow());

                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new CsvImportError(rowNumber, ex.Message));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new UpsertProductsFromCsvResponse(created, updated, errors.Count, errors));
    }

    private static string? Read(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var index) || index >= values.Count)
        {
            return null;
        }

        var value = values[index]?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static Guid? ResolveCategoryId(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> headers,
        IReadOnlyDictionary<string, Guid> byCode,
        IReadOnlyDictionary<string, Guid> byName,
        Guid? fallback)
    {
        var code = Read(values, headers, "categorycode");
        if (!string.IsNullOrWhiteSpace(code) && byCode.TryGetValue(code, out var byCategoryCode))
        {
            return byCategoryCode;
        }

        var name = Read(values, headers, "categoryname");
        if (!string.IsNullOrWhiteSpace(name) && byName.TryGetValue(name, out var byCategoryName))
        {
            return byCategoryName;
        }

        return fallback;
    }

    private static bool TryParseUnit(string? value, out UnitOfMeasure unit)
    {
        unit = UnitOfMeasure.Piece;
        return !string.IsNullOrWhiteSpace(value) &&
               Enum.TryParse(value, ignoreCase: true, out unit);
    }

    private static bool TryParseDecimal(string? value, out decimal? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return false;
        }

        parsed = result;
        return true;
    }

    private static bool TryParseDate(string? value, out DateOnly? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            parsed = dateOnly;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            parsed = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
    }

    private static bool TryParseBool(string? value, out bool parsed)
    {
        parsed = false;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "1" or "yes" or "y" => (parsed = true) || true,
            "false" or "0" or "no" or "n" => (parsed = false) || true,
            _ => bool.TryParse(value, out parsed),
        };
    }

    private static List<string> SplitSuppliers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(['|', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static string NormalizeHeader(string header) =>
        header.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
}
