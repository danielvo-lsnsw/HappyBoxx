using HappyBoxx.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHappyBoxxAuthentication(builder.Configuration, builder.Environment);

var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var hasCorsOrigins = allowedCorsOrigins.Any();
if (hasCorsOrigins)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendCors", policy =>
        {
            policy.WithOrigins(allowedCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (hasCorsOrigins)
{
    app.UseCors("FrontendCors");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
