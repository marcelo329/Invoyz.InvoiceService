---
name: add-endpoint
description: Add a new REST endpoint or CQRS slice (command/query, handler, contract, mapper, controller action, integration test) following this repo's layering. Use when adding or changing an API operation on any entity.
---

# Adding an endpoint to Invoyz

Every operation is one vertical slice through the layers. Four slices exist — Customers, Products, Invoices, InvoiceLines — so copy the closest one: Products for plain CRUD, Invoices for a parent with children, InvoiceLines for a nested sub-resource.

## Layout

```
Contracts/RestAPI/InboundContracts/<Entity>/  request contracts
Contracts/RestAPI/OutboundContracts/          response contracts
Application/CQRS/<Entity>/Commands/Models/    command records
Application/CQRS/<Entity>/Commands/Handlers/
Application/CQRS/<Entity>/Commands/Validators/
Application/CQRS/<Entity>/Queries/Models/     query records
Application/CQRS/<Entity>/Queries/Handlers/
Application/Data/Repositories/SubClasses/     entity repository
Application/Helpers/Mappers.cs                entity -> outbound contract
Invoyz.InvoiceService/Extensions/Mappers.cs   inbound contract -> command
Invoyz.InvoiceService/Controllers/            controller action
```

## Steps

**1. Contract.** A positional `record` in `Contracts`. Never expose entities over HTTP.

**2. Request record.** In `CQRS/<Entity>/{Commands,Queries}/Models/`:

- Query returning data: `IRequest<ErrorOr<T>>`
- Query returning a list: `IRequest<IReadOnlyCollection<T>>`
- Command: `IRequest<Error?>` — null means success
- Create: `IRequest<ErrorOr<Guid>>`

A by-id query should derive from `BaseCQRSWithId`.

**3. Handler.** Constructor-inject the repository. Return `Error.NotFound` / `Error.Conflict` / `Error.Validation` rather than throwing — `BaseController.GetErrorStatusCode` maps those to 404 / 409 / 400, and anything else to 500. MediatR scans the Application assembly, so no registration is needed.

Use string interpolation in error messages. `Error.Conflict("… {id}", value)` does **not** substitute the placeholder — the second argument is the description, and clients end up seeing the literal template.

**3b. Validator,** in `Commands/Validators/`. Validators are discovered by assembly scan (`Application/Bootstrapper.cs`) and run through `ValidationBehavior<,>`, so a new one takes effect with no registration. Failures come back as `Error.Validation` → 400, without an exception.

A request that carries an id should derive from `BaseCQRSWithId`, which lets `BaseController.DeleteAsync` / `GetByIdAsync` reject `Guid.Empty` before MediatR is reached.

**4. Repository method,** if `BaseRepository<TEntity>` doesn't cover it. Add it to the entity's `I<Entity>Repository` and its subclass. **Reads must filter `!a.IsDeleted`** — deletes are soft and there is no global query filter.

**5. Mappers.** Entity → outbound contract in `Application/Helpers/Mappers.cs`; inbound contract → command in `Invoyz.InvoiceService/Extensions/Mappers.cs`.

For anything addressed by id, bind the **route** id, not one from the body, and leave `Id` off the inbound contract entirely — that is how `UpdateCustomerContract` is shaped, and it makes a body/route mismatch unrepresentable rather than merely rejected.

**6. Controller action.** Attributes and a one-line forward to `base`, nothing else:

```csharp
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerContract))]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    => await base.GetByIdAsync<GetCustomerByIdQuery, CustomerContract>(new GetCustomerByIdQuery(id), cancellationToken);
```

`ProducesResponseType` is OpenAPI metadata only — it documents a status code, it does not produce one. Verify the real status with a request; don't read it off Swagger.

A new controller derives from `BaseController` and inherits the route template and `[ApiController]`. Do **not** add a `[Route]` for a top-level resource: a derived `[Route]` replaces `api/v{version:apiVersion}/[controller]` instead of combining with it, silently unversioning every action.

For a **nested** sub-resource the `[Route]` is required, and must spell the version segment out itself — see `InvoiceLinesController`:

```csharp
[Route("api/v{version:apiVersion}/Invoices/{invoiceId:guid}/Lines")]
```

Every action then takes the parent id as a route parameter, and handlers scope their lookup by it so a child id used against the wrong parent returns 404 rather than another parent's data.

**7. Integration test.** In `CustomerControllerIntegrationTests` style: `#region Arrange` / act / `#region Assert`, seed through the DbContext, exercise through `HttpClient`, and verify persistence by reading the DbContext back rather than trusting the response alone. Run it with the `run-tests` skill.

## Notes

- Money on invoices and lines is derived in `Application/Helpers/InvoiceTotals.cs` and never taken from a payload. Any handler touching a line must call `ApplyLineTotals` then `Recalculate` on the parent invoice, and save the invoice.
- **A handler that changes an invoice or one of its lines must publish `InvoiceUpdated`** via the injected `IPublishEndpoint`, after the save succeeds. That event drives PDF regeneration through `InvoiceStatusConsumer`; skipping it leaves the stored document describing a stale invoice. Every existing invoice and line command handler does this — copy one.
- New services belong in `Application/Bootstrapper.BootstrapApplicationService()`, not `Program.cs`, so the worker host gets them too. `Program.cs` is for HTTP concerns only.
- Schema changes need a migration; see the `ef-migrations` skill.
- To exercise an endpoint against a real database, apply migrations first (`dotnet ef database update`), then `dotnet run --project src/Invoyz.InvoiceService`. Skipping the migration gives an empty `invoyz.db` and `no such table` on every request.
