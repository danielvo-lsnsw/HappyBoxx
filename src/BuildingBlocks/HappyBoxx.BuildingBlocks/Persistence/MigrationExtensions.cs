using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyBoxx.BuildingBlocks.Persistence;

public static class MigrationExtensions
{
    /// <summary>Development convenience only; other environments apply migrations from the CD pipeline.</summary>
    public static async Task ApplyMigrationsAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await dbContext.Database.MigrateAsync();
    }
}
