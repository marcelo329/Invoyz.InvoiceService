---
name: ui-conventions
description: Work on the Vue 3 + TypeScript front end in src/Invoyz.InvoiceService.UI — running it, its layering rules, and the proxy/config traps. Use when adding or changing a view, component, service, or anything that calls the API from the browser.
---

# Working on the Invoyz UI

`src/Invoyz.InvoiceService.UI` — Vue 3, TypeScript, Vite, Axios. CRUD over Customers, Products, Invoices and Invoice Lines, plus the invoice PDF download.

## Running it

The API must be running first (see the `run-tests` skill for its commands).

```bash
cd src/Invoyz.InvoiceService.UI && npm install && npm run dev
```

```bash
cd src/Invoyz.InvoiceService.UI && npm run type-check
```

`npm run dev` does **not** type-check. Always run `npm run type-check` (or `npm run build`) before claiming a change works — `vue-tsc` catches what the dev server serves happily.

## Layering rules

```
views/        presentation only
composables/  request state (useCrudList, useDirtyForm)
services/     one repository per resource, over HttpClient
core/http/    HttpClient interface + AxiosHttpClient adapter
core/container.ts   composition root
config/       the only reader of import.meta.env
```

- **Only `AxiosHttpClient` imports Axios.** Everything else depends on the `HttpClient` interface. Adding a transport capability (binary download, headers, retries) means widening that interface, not reaching for Axios in a service.
- **Only `config/appConfig.ts` reads `import.meta.env`.** Settings live in `.env*` files and are validated at startup so a bad value fails loudly.
- **Only `core/container.ts` constructs services.** Components call `injectRequired(key, description)` and receive interfaces, which is what makes them testable over fakes.
- Failures arrive as `ApiError` with a `kind` (`validation` / `notFound` / `conflict` / `network` / `unexpected`) mirroring the status codes `BaseController` produces. Branch on `kind`, never on a raw status.

## Traps that have actually bitten

**Vite reads config only at startup.** After changing `vite.config.ts` or any `.env` file, restart `npm run dev`. A stale dev server silently serves the old proxy config, and API calls come back as `index.html` with a 200 — tables render empty rows with no error.

**The proxy target must match the profile actually running.** The API has no CORS policy, so development calls go through the Vite proxy. `VITE_API_PROXY_TARGET` should be `https://localhost:7280`; the http port answers 307 because of `UseHttpsRedirection`, which defeats the point of proxying.

**`form_input`-style value setting does not trigger `v-model`.** When driving the UI in a browser for verification, prefer clicking the field and typing, or expect a one-render lag before the model updates.

**A 200 is not proof of a JSON body.** Anything that downloads a file should check the content type before handing it to the user, or a misrouted request saves an HTML page under a `.pdf` name.

## Forms

Every modal form uses `useDirtyForm` and passes `:dirty` to `ModalDialog`, so backdrop clicks and Escape prompt before discarding edits. Call `markPristine()` after populating a form, or the dialog treats a freshly opened record as already modified.

Save failures keep the dialog open and render the error inside it — the page-level alert is hidden while a dialog is up. `ConfirmDialog` takes the same `error` prop for refused deletes.
