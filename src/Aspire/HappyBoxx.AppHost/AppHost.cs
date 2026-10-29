var builder = DistributedApplication.CreateBuilder(args);

// Databases are external (SQL Server / LocalDB); connection strings come from AppHost configuration.
var catalogDb = builder.AddConnectionString("catalogdb");
var inventoryDb = builder.AddConnectionString("inventorydb");
var identityDb = builder.AddConnectionString("identitydb");

var identityApi = builder.AddProject<Projects.HappyBoxx_Identity_Api>("identity-api")
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health");

var catalogApi = builder.AddProject<Projects.HappyBoxx_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(identityApi).WaitFor(identityApi)
    .WithHttpHealthCheck("/health");

var inventoryApi = builder.AddProject<Projects.HappyBoxx_Inventory_Api>("inventory-api")
    .WithReference(inventoryDb)
    .WithReference(identityApi).WaitFor(identityApi)
    .WithHttpHealthCheck("/health");

var gateway = builder.AddProject<Projects.HappyBoxx_Gateway>("gateway")
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(inventoryApi).WaitFor(inventoryApi)
    .WithReference(identityApi).WaitFor(identityApi)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

if (builder.Configuration["HappyBoxx:Authentication:Mode"] == "Entra")
{
    var authority = builder.Configuration["HappyBoxx:Authentication:Authority"]
        ?? throw new InvalidOperationException("HappyBoxx:Authentication:Authority is required when Entra authentication is enabled.");
    var audience = builder.Configuration["HappyBoxx:Authentication:Audience"]
        ?? throw new InvalidOperationException("HappyBoxx:Authentication:Audience is required when Entra authentication is enabled.");
    var validIssuer = builder.Configuration["HappyBoxx:Authentication:ValidIssuer"]
        ?? throw new InvalidOperationException("HappyBoxx:Authentication:ValidIssuer is required when Entra authentication is enabled.");

    foreach (var service in new[] { catalogApi, inventoryApi, identityApi, gateway })
    {
        service.WithEnvironment("Authentication__Mode", "Entra")
            .WithEnvironment("Authentication__Authority", authority)
            .WithEnvironment("Authentication__Audience", audience)
            .WithEnvironment("Authentication__ValidIssuer", validIssuer);
    }
}

builder.AddViteApp("admin-portal", "../../Web/admin-portal")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithHttpEndpoint(port: 5173)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
