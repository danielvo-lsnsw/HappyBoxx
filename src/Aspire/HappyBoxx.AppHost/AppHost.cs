var builder = DistributedApplication.CreateBuilder(args);

// Databases are external (SQL Server / LocalDB); connection strings come from AppHost configuration.
var catalogDb = builder.AddConnectionString("catalogdb");
var inventoryDb = builder.AddConnectionString("inventorydb");
var identityDb = builder.AddConnectionString("identitydb");

var catalogApi = builder.AddProject<Projects.HappyBoxx_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithHttpHealthCheck("/health");

var inventoryApi = builder.AddProject<Projects.HappyBoxx_Inventory_Api>("inventory-api")
    .WithReference(inventoryDb)
    .WithHttpHealthCheck("/health");

var identityApi = builder.AddProject<Projects.HappyBoxx_Identity_Api>("identity-api")
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health");

var gateway = builder.AddProject<Projects.HappyBoxx_Gateway>("gateway")
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(inventoryApi).WaitFor(inventoryApi)
    .WithReference(identityApi).WaitFor(identityApi)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddViteApp("admin-portal", "../../Web/admin-portal")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
