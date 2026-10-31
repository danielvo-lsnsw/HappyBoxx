using HappyBoxx.Billing.Api.Infrastructure.Email;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("happyboxxdb")
    ?? builder.Configuration.GetConnectionString("billingdb")
    ?? throw new InvalidOperationException("Connection string 'happyboxxdb' is not configured.");

builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddHappyBoxxAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddInvoiceEmail(builder.Configuration);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BillingDbContext>((sp, options) => options
    .UseSqlServer(
        connectionString,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.Schema))
    .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
    .UseAsyncSeeding((context, _, cancellationToken) => BillingSeeder.SeedAsync(context, cancellationToken)));
builder.EnrichSqlServerDbContext<BillingDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync<BillingDbContext>();
}

app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api/v1").RequireAuthorization(HappyBoxxPolicies.Staff));

await app.RunAsync();
