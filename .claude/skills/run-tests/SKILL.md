---
name: run-tests
description: Build and run the Invoyz integration tests, including a single test or class. Use whenever tests need to be executed or a test failure needs reproducing in this repo — `dotnet test` does not work here.
---

# Running the Invoyz tests

`dotnet test` fails on this repo. `Invoyz.InvoiceService.Tests` uses xunit.v3, which runs on Microsoft.Testing.Platform, and the .NET 10 SDK refuses the VSTest path:

```
error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later.
```

Build the project, then run its executable.

## Full suite

```bash
dotnet build src/Invoyz.InvoiceService.Tests/Invoyz.InvoiceService.Tests.csproj && ./src/Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe
```

## A single test or class

Wildcards are supported at either end. Simple filters (`-method`, `-class`) and query filters (`-filter`) cannot be mixed.

```bash
./src/Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe -method "*Put_WithNoChanges*"
```

```bash
./src/Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe -class "Invoyz.InvoiceService.Tests.CustomerControllerIntegrationTests"
```

## Reading the output

The API logs through Serilog to the same console, so the run is dominated by request logs and SQL. Filter to what matters:

```bash
./src/Invoyz.InvoiceService.Tests/bin/Debug/net10.0/Invoyz.InvoiceService.Tests.exe 2>&1 | grep -E "\[FAIL\]|Assert\.|Expected:|Actual:|Total:"
```

For one failure's full detail, re-run that test alone with `-method` and read the whole output — the exception and stack trace are interleaved with the request log.

## If the build fails with a file lock

```
MSB3027: Could not copy ... The file is locked by: "Invoyz.InvoiceService.Tests (NNNNN)"
```

A test host is still running, almost always a Visual Studio debug session paused on an exception. **Do not kill it** — ask the user to stop debugging (Shift+F5), then rebuild.

## Interpreting failures

When the solution builds, all 94 tests pass. They are written against intended behaviour, so a failure reports a real product bug — report the cause and fix the code; never weaken an assertion to make a test pass.

**Check the build first.** The contracts moved to `Contracts.RestAPI.*`, and test files still importing the old `Contracts.InboundContracts` namespace fail to compile — a build error, not a test failure, and the two read very differently in the output.

Green is not proof the flow is correct. The suite misses issues listed under "Known gaps" in CLAUDE.md, notably paging order, because some tests build their expectation the same way the code computes the result. To check a flow properly, exercise the running API:

```bash
dotnet ef database update --project src/Invoyz.InvoiceService.Infra --startup-project src/Invoyz.InvoiceService
```

```bash
dotnet run --project src/Invoyz.InvoiceService
```

then drive `http://localhost:5271/api/v1/Customers` with curl. Without the migration step the API starts fine but every request fails with `no such table: Customers` — SQLite creates an empty file on connect.

If you create a scratch database while testing, leave it in place or re-run `database update`; deleting it leaves the next run with an empty file and that confusing error.
