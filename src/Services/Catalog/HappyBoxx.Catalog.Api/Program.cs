using HappyBoxx.BuildingBlocks;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Persistence;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CatalogDbContext>((sp, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("catalogdb")
        ?? throw new InvalidOperationException("Connection string 'catalogdb' is not configured."))
    .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
    .UseAsyncSeeding((context, _, cancellationToken) => CatalogSeeder.SeedAsync(context, cancellationToken)));
builder.EnrichSqlServerDbContext<CatalogDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync<CatalogDbContext>();
}

app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api/v1"));

await app.RunAsync();
