using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.UnitTests;

internal static class TestDbContextFactory
{
    public static CatalogDbContext Create() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
