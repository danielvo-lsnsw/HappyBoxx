using HappyBoxx.BuildingBlocks;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Persistence;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using HappyBoxx.Identity.Api.Infrastructure.Email;
using HappyBoxx.Identity.Api.Security;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddHappyBoxxAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddTransient<IClaimsTransformation, DatabaseRoleClaimsTransformation>();
builder.Services.AddStaffInvitationEmail(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<IdentityDbContext>((sp, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("identitydb")
        ?? throw new InvalidOperationException("Connection string 'identitydb' is not configured."))
    .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));
builder.EnrichSqlServerDbContext<IdentityDbContext>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync<IdentityDbContext>();
}

app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api/v1").RequireAuthorization());

await app.RunAsync();