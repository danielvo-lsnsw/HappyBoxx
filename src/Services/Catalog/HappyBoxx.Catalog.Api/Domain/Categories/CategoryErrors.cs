using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Catalog.Api.Domain.Categories;

public static class CategoryErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Category.NotFound", $"Category '{id}' was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Category.NameAlreadyExists", $"A category named '{name}' already exists.");
}
