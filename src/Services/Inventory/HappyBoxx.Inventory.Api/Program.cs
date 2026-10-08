using HappyBoxx.BuildingBlocks;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using HappyBoxx.Inventory.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddHappyBoxxAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<InventoryDbContext>((sp, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("inventorydb")
        ?? throw new InvalidOperationException("Connection string 'inventorydb' is not configured."))
    .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));
builder.EnrichSqlServerDbContext<InventoryDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync<InventoryDbContext>();
}

app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api/v1").RequireAuthorization(HappyBoxxPolicies.Staff));

await app.RunAsync();
