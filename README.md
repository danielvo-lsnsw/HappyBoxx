# HappyBoxx

Warehouse platform for selling and delivering vegetables, fruits and food containers.

- **Admin portal**: manage categories, product pricing/suppliers, and stock. *(in progress)*
- **Buyer storefront**: browse items and place orders. *(planned)*

See [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md) for conventions.

## Architecture

```mermaid
flowchart LR
    admin[Admin Portal<br/>React + Vite] --> gw[Gateway<br/>YARP]
    gw -->|/api/v1/categories, /products| catalog[Catalog API]
    gw -->|/api/v1/stock| inventory[Inventory API]
    gw -->|/api/v1/invoices| billing[Billing API]
    catalog --> catdb[(catalogdb)]
    inventory --> invdb[(inventorydb)]
```

The whole system is orchestrated locally by **.NET Aspire**, which provides service discovery, health checks and an OpenTelemetry dashboard.

| Project | Purpose |
|---|---|
| `src/Aspire/HappyBoxx.AppHost` | Starts everything locally |
| `src/Aspire/HappyBoxx.ServiceDefaults` | Telemetry, health, resilience, discovery |
| `src/BuildingBlocks/HappyBoxx.BuildingBlocks` | Result/Error, endpoint discovery, validation filter, paging, auditing |
| `src/Gateway/HappyBoxx.Gateway` | Single entry point for front-ends |
| `src/Services/Catalog` | Categories, products, pricing tiers and suppliers |
| `src/Services/Inventory` | Stock levels and adjustments |
| `src/Services/Billing` | Customer invoices and GST totals |
| `src/Web/admin-portal` | Admin SPA |

### Invoices

Invoices are created from customer/address and line-item details, saved in the billing database, and can be printed or saved as PDF from **Orders → Invoices**. Entered prices include GST; the invoice shows the included 10% GST separately.

## Prerequisites

- .NET SDK 10 (see `global.json`)
- Node.js 20.19+ (or 22.12+)
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`), or update the connection strings in `src/Aspire/HappyBoxx.AppHost/appsettings.Development.json`

## Run

```powershell
dotnet tool restore
dotnet run --project src/Aspire/HappyBoxx.AppHost
```

Open the dashboard link printed in the console. From there you can reach the admin portal, the gateway, and each API's Scalar docs at `/scalar`. In Development, databases are created, migrated and seeded on startup.

### Catalog seed source

Catalog startup seeding now imports normalized category/product/supplier data from:

- `src/Services/Catalog/HappyBoxx.Catalog.Api/Infrastructure/Persistence/SeedData/price-list-import.json`

This file is generated from the business pricing spreadsheet and includes:
- category code + name
- product code (SKU), legacy code, pack details, active state
- sell near, sell far, buy price, effective date
- multi-supplier links with a default supplier

### Product operations status

- Phase 1: bulk product actions and CSV upsert by SKU
- Phase 2: supplier intelligence (last buy price on product suppliers) and product change history
- Phase 3:
  - Reorder intelligence (min/max levels, demand-based reorder suggestions)
  - Product profitability dashboard (gross margins, trends, low/negative margin alerts, category heatmap)

> If your machine's security policy blocks unsigned `.exe` files, set `$env:HAPPYBOXX_NO_APPHOST='true'` before building or running. The apps then run through `dotnet <dll>`.

## Test

```powershell
dotnet test --solution HappyBoxx.slnx
cd src/Web/admin-portal; npm test; npm run lint
```

## Add a migration

```powershell
dotnet ef migrations add <Name> -p src/Services/Catalog/HappyBoxx.Catalog.Api -o Infrastructure/Persistence/Migrations
```
