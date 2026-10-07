using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Catalog.Api.Domain.Products;

public static class ProductErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"Product '{id}' was not found.");

    public static Error SkuAlreadyExists(string sku) =>
        Error.Conflict("Product.SkuAlreadyExists", $"A product with SKU '{sku}' already exists.");

    public static Error CategoryNotFound(Guid categoryId) =>
        Error.Validation("Product.CategoryNotFound", $"Category '{categoryId}' does not exist.");
}
