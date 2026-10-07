using HappyBoxx.Catalog.Api.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence;

internal static class CatalogSeeder
{
    private static readonly (string Name, string Description)[] DefaultCategories =
    [
        ("Vegetables", "Fresh vegetables"),
        ("Fruits", "Fresh fruits"),
        ("Food Containers", "Takeaway boxes, trays and packaging"),
    ];

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var categories = context.Set<Category>();

        if (await categories.AnyAsync(cancellationToken))
        {
            return;
        }

        categories.AddRange(DefaultCategories.Select(c => Category.Create(c.Name, c.Description)));
        await context.SaveChangesAsync(cancellationToken);
    }
}
