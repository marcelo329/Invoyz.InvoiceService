# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build Invoyz.InvoiceService.slnx
```

```bash
dotnet run --project Invoyz.InvoiceService
```

Runs on http://localhost:5271 / https://localhost:7280, Swagger at `/swagger` (Development only).

### Tests

**`dotnet test` does not work here.** The test project uses xunit.v3, which runs on Microsoft.Testing.Platform; the .NET 10 SDK rejects the VSTest path with `Testing with VSTest target is no longer supported`. Build, then run the executable:

```bash
dotnet build Invoyz.InvoiceService.Tests/Invoyz.InvoiceService.Tests.csproj && ./Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe
```

A single test or class (wildcards allowed, simple and query filters cannot be mixed):

```bash
./Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe -method "*GetById_WithUnknownId*"
```

```bash
./Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe -class "Invoyz.InvoiceService.Tests.CustomerControllerIntegrationTests"
```

If the build fails with `MSB3027 ... file is locked by: "Invoyz.InvoiceService.Tests"`, a test host from Visual Studio is still alive — the user is mid-debug. Ask them to stop it rather than killing the process.

### Migrations

`AppDbContext` lives in **Application**, migrations live in **Infra**, so `Infra/Bootstrap.cs` pins `MigrationsAssembly("Invoyz.InvoiceService.Infra")`. Every `dotnet ef` command needs both projects:

```bash
dotnet ef migrations add <Name> --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

```bash
dotnet ef database update --project Invoyz.InvoiceService.Infra --startup-project Invoyz.InvoiceService
```

## Architecture

Clean architecture + CQRS over MediatR. Request flow:

```
CustomersController        thin; [HttpX] + [ProducesResponseType] only
  -> BaseController        generic dispatch, ErrorOr -> status code
    -> MediatR             IRequest -> IRequestHandler
      -> I<X>Repository    BaseRepository<TEntity> + entity-specific subclass
        -> AppDbContext
```

Dependencies: `Invoyz.InvoiceService -> Application, Contracts, Infra`; `Infra -> Application`; `Application -> Contracts, Domains`.

### BaseController is the whole HTTP layer

`Controllers/BaseController.cs` carries the route template `api/v{version:apiVersion}/[controller]` and `[ApiController]`, and exposes five generic dispatch methods (`GetAsync`, `GetByIdAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`). Controllers only declare attributes and forward to `base`.

Route attributes do **not** combine across inheritance — a `[Route]` on a derived controller replaces the base template rather than extending it. Don't add one unless you are deliberately declaring a nested resource, and then spell the version segment out yourself or the endpoint is silently unversioned. `InvoiceLinesController` is the only controller that does this:

```csharp
[Route("api/v{version:apiVersion}/Invoices/{invoiceId:guid}/Lines")]
```

`ErrorOr` failures map to status codes in `GetErrorStatusCode`: `Conflict` → 409, `Validation` → 400, `NotFound` → 404, everything else → 500. Commands return `Error?` (null = success); queries return `ErrorOr<T>`. Success dispatch differs per verb: `Ok()` for PUT, `NoContent()` for DELETE, `Created($"{Request.Path}/{id}", id)` for POST — the last stays generic so the base class never names a derived controller.

`DeleteAsync` and `GetByIdAsync` additionally guard `command.Id == default` and return `ValidationProblem` before touching MediatR, which is why they work without a validator. Requests carrying an id derive from `BaseCQRSWithId` to make that guard possible.

### Validation

FluentValidation runs through a MediatR pipeline behavior — `Application/ValidatorBehavior.cs` (`ValidationBehavior<,>`), registered via `cfg.AddOpenBehavior(...)` in `Program.cs`, with validators registered by `Application/Bootstrapper.cs`.

The behavior does **not** throw on failure. It builds the handler's own response type — `Error?` for commands, `ErrorOr<T>` via the implicit `List<Error>` conversion for queries — carrying `Error.Validation`, which maps to 400. Throwing `ValidationException` instead would surface as 500, since `GetErrorStatusCode` has no case for it.

### Repository conventions

`BaseRepository<TEntity>` constrains to `BaseEntity` (`Id`, `IsDeleted`, `DeletedAt?`, `CreatedAt`, `LastModifiedAt?`). Deletes are **soft** — `DeleteAsync` sets `IsDeleted`, and reads filter `!a.IsDeleted` explicitly in each query. There is no EF global query filter, so any new read must repeat that predicate.

Soft delete also means uniqueness is scoped to live rows: a deleted customer releases its VAT number and a deleted invoice releases its number. The unique index on `Invoices.InvoiceNumber` is filtered (`"IsDeleted" = 0`) to match.

### Invoice money

`Application/Helpers/InvoiceTotals.cs` owns every derived amount. `ApplyLineTotals` sets `LineTotal`/`LineTax` on a line; `Recalculate` rolls live lines up into the invoice `SubTotal`/`TaxTotal`/`GrandTotal`. Amounts are never accepted from a payload.

Any handler that touches a line must call both and then save **the invoice** — the line is attached to the tracked invoice graph, so one `UpdateAsync` persists the line and the refreshed rollup together. Skipping `Recalculate` leaves the stored totals silently disagreeing with the lines.

Line `UnitPrice`/`TaxRate` are optional on input and snapshot from the product when omitted, so a line records the price that applied when it was raised rather than following later product edits.

### Adding a vertical slice

Under `Application/CQRS/<Entity>/{Commands,Queries}/{Models,Handlers,Validators}`: a request record (`IRequest<ErrorOr<T>>` or `IRequest<Error?>`), its handler, optionally a FluentValidation validator. Inbound contracts go in `Contracts/InboundContracts/`, outbound in `Contracts/InboundContracts/OutboundContracts/`. Contract→command mapping lives in `Invoyz.InvoiceService/Extensions/Mappers.cs`; entity→contract mapping in `Application/Helpers/Mappers.cs`.

MediatR is registered by assembly scan from `GetCustomersQuery`, so handlers in the Application assembly are picked up automatically. Validators are picked up the same way, so a new one takes effect with no registration.

Route ids are authoritative: `MapToUpdateCustomerCommand(contract, id)` binds the **route** id, and `UpdateCustomerContract` deliberately has no `Id` so a body/route mismatch is unrepresentable. Keep that property off inbound contracts.

### Integration tests

`Tests/Boostrap/CustomWebApplicationFactory.cs` (note the folder spelling) boots the real API, replaces the configured SQLite file with one `DataSource=:memory:` connection held open for the fixture's lifetime, and applies the Infra migrations in `CreateHost`.

Two constraints worth knowing before editing it: building a second `ServiceProvider` inside `ConfigureServices` opens a *different* `:memory:` database, so schema work must use the host's provider; and the factory's own `UseSqlite` must repeat `MigrationsAssembly`, or `Migrate()` finds no migrations and silently creates nothing.

Tests in a class share one fixture and one database, so each test reseeds through its own seed helper, which clears the relevant tables first. Each test class gets its own fixture, hence its own in-memory database.

## Known gaps

The 94 tests pass. These are open issues the suite does **not** catch — verified against the running API, not inferred. Tests here encode intended behaviour, so if one fails, fix the code rather than weakening the assertion.

- **Paging ignores global ordering.** `GetListAsync` applies `Skip`/`Take` with no `OrderBy`, and `GetCustomersQueryHandler` sorts only the page it received. Which rows land on which page is undefined; EF logs a warning every run. The `Get_*` tests can't catch it because they build expectations the same way the code does.
- **`page=0` silently duplicates page 1.** Controller params are `ushort`, so `-1` and values above 65535 fail model binding with a 400 that reads like validation but isn't. `0` binds fine, and `Skip((0-1) * n)` becomes a negative `OFFSET` that SQLite treats as 0. There is no `GetCustomersQueryValidator`, and `GetCustomersQuery` still declares `int Page, int PageSize`, so the `ushort` guard exists only at the controller edge.
- **Entities redeclare `Id`**, hiding `BaseEntity.Id` (CS0108 on all four).
- **`OnModelCreating` configures only indexes** — column shapes still come entirely from conventions.
