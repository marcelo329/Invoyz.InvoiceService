# Invoyz.InvoiceService

## Description
This is a full stack application with a UI built in Vue.js and a BFF built in Csharp with .NET10.0

## Internal architecture

There are six projects. This implements a Clean architecture with CQRS, facilitated by Mediator pattern.

### Invoyz.InvoiceService
Restfull webAPI built using the OpenAPI pattern with Swagger. Uses serilog , mediatr, and API versioning.
### Invoyz.InvoiceService.Application 
Has the use cases for domain logic, and validation for each properties, with guard clauses. Has the handlers
for the mediator commands and queries. Sits between Domain and adapters layers.
### Invoyz.InvoiceService.Contracts
As the inbound and outbound contracts used to integrate with the WebAPI.
### Invoyz.InvoiceService.Domains
As the domain entities used as foundations for the database relations.
### Invoyz.InvoiceService.Infra
Sits in the outer layer of the clean architecture and has the different services implementations for I/O.
### Invoyz.InvoiceService.Tests
Integration tests run agains the API using a virtual webserver.

## Project dependencies

```
Invoyz.InvoiceService  ->  Application, Contracts, Infra
Infra                  ->  Application
Application            ->  Contracts, Domains
Contracts              ->  (none)
Domains                ->  (none)
Tests                  ->  Invoyz.InvoiceService, Application
```

## Tech stack

| Concern | Package | Version |
| --- | --- | --- |
| Target framework | .NET | 10.0 |
| Persistence | Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 |
| CQRS dispatch | MediatR | 14.2.0 |
| Validation | FluentValidation (+ DependencyInjectionExtensions) | 12.1.1 |
| Result type | ErrorOr | 2.1.1 |
| Logging | Serilog (console sink, span enricher) | 4.4.0 |
| API versioning | Asp.Versioning.Http | 10.2.3 |
| OpenAPI | Swashbuckle.AspNetCore | 10.2.3 |
| Tests | xunit.v3 + Microsoft.AspNetCore.Mvc.Testing | 4.0.1 / 10.0.12 |

## Prerequisites

- .NET 10 SDK
- EF Core CLI tools, for migrations:

```bash
dotnet tool install --global dotnet-ef
```

## Getting started

```bash
dotnet restore Invoyz.InvoiceService.slnx
```

```bash
dotnet ef database update --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

```bash
dotnet run --project Invoyz.InvoiceService
```

| Profile | URL |
| --- | --- |
| http | http://localhost:5271 |
| https | https://localhost:7280 |
| Swagger UI | /swagger (Development only) |

## Configuration

The SQLite connection string is read from `ConnectionStrings:Sqlite` in `Invoyz.InvoiceService/appsettings.json`:

```json
"ConnectionStrings": {
  "Sqlite": "Data Source=invoyz.db"
}
```

The path is relative to the process working directory, so running from the repository root and from the project folder produce database files in different places.

## Database and migrations

`AppDbContext` lives in **Application** (`Data/AppDbContext.cs`), while the migrations live in **Infra** (`Migrations/`). Because those are two different assemblies, `Infra/Bootstrap.cs` pins the migrations assembly explicitly:

```csharp
services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(
        configuration.GetConnectionString("Sqlite"),
        sqlite => sqlite.MigrationsAssembly("Invoyz.InvoiceService.Infra")));
```

Every `dotnet ef` command therefore needs both projects — `--project` for where migrations are written, `--startup-project` for where configuration and the host live:

```bash
dotnet ef migrations add <Name> --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

```bash
dotnet ef database update --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

Entities are `Customers`, `Products`, `Invoices` and `InvoiceLines`, all deriving from `BaseEntity` (`Id`, `IsDeleted`, `DeletedAt`, `CreatedAt`, `LastModifiedAt`).

`AppDbContext.OnModelCreating` adds a **filtered** unique index on `Invoices.InvoiceNumber` (`"IsDeleted" = 0`), so uniqueness matches what the handlers enforce and a soft-deleted invoice does not reserve its number forever. `HasFilter` is a relational API, which is why Application references `Microsoft.EntityFrameworkCore.Relational`.

## Running the tests

`Invoyz.InvoiceService.Tests` boots the real API through `WebApplicationFactory`. `CustomWebApplicationFactory` swaps the configured SQLite file for a single `DataSource=:memory:` connection held open for the lifetime of the fixture, and applies the Infra migrations to it in `CreateHost`, so every run starts from the real schema.

The project uses **xunit.v3**, which runs on Microsoft.Testing.Platform rather than VSTest. On the .NET 10 SDK `dotnet test` fails with `Testing with VSTest target is no longer supported`. Build and run the test executable directly instead:

```bash
dotnet build Invoyz.InvoiceService.Tests/Invoyz.InvoiceService.Tests.csproj
```

```bash
./Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe
```

Tests within a class share one fixture and therefore one database, so each test reseeds through its own seed helper, which clears the relevant tables first.

Four integration test classes — `CustomerControllerIntegrationTests`, `ProductControllerIntegrationTests`, `InvoiceControllerIntegrationTests`, `InvoiceLineControllerIntegrationTests` — cover every endpoint: 94 cases in total. Each class gets its own fixture and therefore its own in-memory database.

Note that the integration tests use their own in-memory database. Running the API itself still needs `dotnet ef database update` first; without it SQLite creates an empty `invoyz.db` on connect and every request fails with `no such table: Customers`.

## API

All routes are versioned through `BaseController`'s `api/v{version:apiVersion}/[controller]` template. The default version is `1.0`.

Four resources follow the same shape. `page` and `pageSize` are `ushort`, defaulting to 1 and 10; `Guid.Empty` in a route returns 400; POST returns 201 with a `Location` header; DELETE returns 204 and soft-deletes.

| Resource | Routes |
| --- | --- |
| Customers | `/api/v1/Customers`, `/api/v1/Customers/{id}` |
| Products | `/api/v1/Products`, `/api/v1/Products/{id}` |
| Invoices | `/api/v1/Invoices`, `/api/v1/Invoices/{id}` |
| Invoice lines | `/api/v1/Invoices/{invoiceId}/Lines`, `/api/v1/Invoices/{invoiceId}/Lines/{id}` |

On PUT the **route** id is authoritative; update contracts carry no `Id`.

### Resource rules

**Customers** — VAT number unique among live customers; 409 on collision.

**Products** — `UnitPrice >= 0`, `TaxRate` between 0 and 1. Deleting a product still referenced by a live invoice line returns 409.

**Invoices** — created with at least one line in the same payload; an empty `lines` array is a 400. `InvoiceNumber` is unique among live invoices (enforced by a filtered unique index as well as the handler), `CustomerId` must resolve, and `DueDate` cannot precede `IssueDate`. `Status` is one of `Draft`, `Sent`, `Paid`, `Overdue`, accepted case-insensitively and stored canonically. Deleting an invoice cascades the soft delete to its lines.

**Invoice lines** — addressed only through their parent invoice; a line id used against the wrong invoice reads as 404. `UnitPrice` and `TaxRate` are optional and snapshot from the product when omitted, so a line keeps the price that applied when it was raised. Deleting the last remaining line returns 409 — delete the invoice instead.

### Money

Line and invoice money is derived in `Application/Helpers/InvoiceTotals.cs` and never read from a payload:

```
LineTotal  = Quantity * UnitPrice
LineTax    = LineTotal * TaxRate
SubTotal   = sum of live LineTotal
TaxTotal   = sum of live LineTax
GrandTotal = SubTotal + TaxTotal
```

Rounded to 2 decimal places, away from zero. Every line mutation recalculates its invoice, so the stored rollup cannot drift from the lines it summarises.

Soft-deleted rows are excluded from all reads, and the unique values they held (customer VAT number, invoice number) become reusable.

`BaseController` maps `ErrorOr` failures to status codes: `Conflict` → 409, `Validation` → 400, `NotFound` → 404, everything else → 500.

## Validation

Request validation is FluentValidation driven through a MediatR pipeline behavior (`Application/ValidatorBehavior.cs`), registered with `cfg.AddOpenBehavior(...)` in `Program.cs`; validators are discovered by assembly scan in `Application/Bootstrapper.cs`, so a new validator needs no registration.

The behavior returns the handler's own response type carrying `Error.Validation` rather than throwing — throwing would surface as a 500, since the status-code mapping has no case for `ValidationException`.

Each slice has Create/Update/Delete validators. Paging parameters are still unvalidated (see CLAUDE.md, Known gaps).

