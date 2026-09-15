---
name: ef-migrations
description: Add or apply EF Core migrations in this repo, where the DbContext and the migrations live in different assemblies. Use when the schema changes, a migration must be generated, reverted, or applied, or `dotnet ef` reports an assembly mismatch.
---

# EF Core migrations in Invoyz

The split that makes these commands unusual:

| Thing | Project |
| --- | --- |
| `AppDbContext` | `Invoyz.InvoiceService.Application` (`Data/AppDbContext.cs`) |
| Entities | `Invoyz.InvoiceService.Domains` |
| Migrations | `Invoyz.InvoiceService.Infra` (`Migrations/`) |
| Host + configuration | `Invoyz.InvoiceService` |

EF defaults the migrations assembly to the context's assembly (Application). `Infra/Bootstrap.cs` overrides it:

```csharp
services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(
        configuration.GetConnectionString("Sqlite"),
        sqlite => sqlite.MigrationsAssembly("Invoyz.InvoiceService.Infra")));
```

So every command needs `--project` (where migrations are written) **and** `--startup-project` (where config and the host live).

## Commands

```bash
dotnet ef migrations add <Name> --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

```bash
dotnet ef database update --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

```bash
dotnet ef migrations remove --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

`--output-dir` is unnecessary; `Migrations/` is already the default and exists.

Prerequisites, if `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`. `Microsoft.EntityFrameworkCore.Design` must be referenced by the **startup** project.

## After generating a migration

1. Read the generated `Up`/`Down` before applying. SQLite has limited `ALTER TABLE` support, so EF rebuilds tables for many changes — check the migration doesn't silently drop data.
2. The integration test fixture applies these same migrations to its in-memory database (`CustomWebApplicationFactory.CreateHost`), so run the tests — a broken migration surfaces there immediately. See the `run-tests` skill.
3. `AppDbContext` has no `OnModelCreating`, so the schema comes entirely from conventions. If a column shape matters, configure it explicitly rather than accepting the convention default.
4. A migration that comes out with empty `Up`/`Down` means the model already matches the snapshot — someone regenerated an earlier migration instead of adding one. Remove it (`dotnet ef migrations remove`) rather than leaving a no-op in the history.

Changing a property's nullability is a schema change even though nothing about the table list changes. Entity edits like `DateTimeOffset` → `DateTimeOffset?` need a migration, or inserts hit a NOT NULL constraint at runtime while the code compiles cleanly.

## Connection string

Read from `ConnectionStrings:Sqlite` in `Invoyz.InvoiceService/appsettings.json`. `migrations add` works without it; `database update` does not. The `Data Source=` path is relative to the working directory, so the same command run from different folders touches different database files.
