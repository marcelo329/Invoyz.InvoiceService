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

## Running the tests

`Invoyz.InvoiceService.Tests` boots the real API through `WebApplicationFactory`. `CustomWebApplicationFactory` swaps the configured SQLite file for a single `DataSource=:memory:` connection held open for the lifetime of the fixture, and applies the Infra migrations to it in `CreateHost`, so every run starts from the real schema.

The project uses **xunit.v3**, which runs on Microsoft.Testing.Platform rather than VSTest. On the .NET 10 SDK `dotnet test` fails with `Testing with VSTest target is no longer supported`. Build and run the test executable directly instead:

```bash
dotnet build Invoyz.InvoiceService.Tests/Invoyz.InvoiceService.Tests.csproj
```

```bash
./Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe
```

Tests within a class share one fixture and therefore one database, so each test reseeds via `SeedTwentyCustomersAsync`, which clears the `Customers` table first.

`CustomerControllerIntegrationTests` covers all five endpoints — 21 test methods, 27 cases including theories.

Note that the integration tests use their own in-memory database. Running the API itself still needs `dotnet ef database update` first; without it SQLite creates an empty `invoyz.db` on connect and every request fails with `no such table: Customers`.

## API

All routes are versioned through `BaseController`'s `api/v{version:apiVersion}/[controller]` template. The default version is `1.0`.

| Method | Route | Success | Notes |
| --- | --- | --- | --- |
| GET | `/api/v1/Customers?page=1&pageSize=10` | 200 | `page` and `pageSize` are `ushort`, defaulting to 1 and 10 |
| GET | `/api/v1/Customers/{id}` | 200 | `Guid.Empty` → 400 |
| POST | `/api/v1/Customers` | 201 + `Location` | Body: `CreateCustomerContract` |
| PUT | `/api/v1/Customers/{id}` | 200 | Body: `UpdateCustomerContract`; the **route** id is authoritative |
| DELETE | `/api/v1/Customers/{id}` | 204 | Soft delete: sets `IsDeleted`; `Guid.Empty` → 400 |

Soft-deleted customers are excluded from both reads, and their VAT number becomes reusable.

`BaseController` maps `ErrorOr` failures to status codes: `Conflict` → 409, `Validation` → 400, `NotFound` → 404, everything else → 500.

## Validation

Request validation is FluentValidation driven through a MediatR pipeline behavior (`Application/ValidatorBehavior.cs`), registered with `cfg.AddOpenBehavior(...)` in `Program.cs`; validators are discovered by assembly scan in `Application/Bootstrapper.cs`, so a new validator needs no registration.

The behavior returns the handler's own response type carrying `Error.Validation` rather than throwing — throwing would surface as a 500, since the status-code mapping has no case for `ValidationException`.

Current rules: `CreateCustomerCommandValidator` requires Name, Address, Email (well-formed) and VatNumber; `UpdateCustomerCommandValidator` adds a non-empty `Id` on top of those. Paging parameters are not yet validated.

