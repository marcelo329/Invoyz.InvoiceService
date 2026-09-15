# Invoyz.InvoiceService.UI

Vue 3 + TypeScript + Vite front end for the Invoyz BFF. Provides CRUD for Customers,
Products, Invoices and Invoice Lines.

## Running

The API must be running first (see the repository README):

```bash
dotnet run --project src/Invoyz.InvoiceService --launch-profile http
```

Then, from this folder:

```bash
npm install
```

```bash
npm run dev
```

The app serves on <http://localhost:5173>.

```bash
npm run type-check
```

```bash
npm run build
```

## Configuration

Nothing reads `import.meta.env` except `src/config/appConfig.ts`. Settings live in
`.env` files and are validated at startup, so a missing or malformed value fails
immediately instead of surfacing as `undefined` inside a request.

| Variable | Purpose |
| --- | --- |
| `VITE_API_BASE_URL` | Base path or origin for API calls |
| `VITE_API_TIMEOUT_MS` | Axios request timeout |
| `VITE_DEFAULT_PAGE_SIZE` | Rows per page in every list |
| `VITE_APP_TITLE` | Shown in the sidebar and document title |
| `VITE_INVOICE_STATUSES` | Comma separated status options |
| `VITE_API_PROXY_TARGET` | Dev only: where Vite proxies `/api` |

`.env` holds defaults, `.env.development` and `.env.production` override per
environment, and `.env.local` (gitignored) is for machine specific overrides.

**The API declares no CORS policy**, so in development every call goes through the
Vite proxy and stays same origin. Production sets `VITE_API_BASE_URL` to the deployed
BFF origin instead; if the UI is served from a different origin there, the API needs a
CORS policy added.

## Structure

```
src/
  config/       settings loading and validation
  core/
    http/       HttpClient interface + Axios adapter
    errors/     ApiError, problem-details translation
    container   composition root (provide/inject wiring)
    format      shared date/money formatting
  domain/       TypeScript models mirroring the API contracts
  services/     repositories, one per resource
  composables/  useCrudList: list/save/delete state
  components/   table, modal, confirm, pager, field, alert
  views/        one per section, plus invoice detail
  pdf/          InvoicePdfGenerator port + not-implemented adapter
  router/
```

### How the layers depend on each other

Components depend on interfaces resolved through Vue's `provide` / `inject`, never on
concrete classes. `src/core/container.ts` is the only place a service is constructed,
so mounting a view over fakes means providing different values for the same keys.

`AxiosHttpClient` is the only file that imports Axios. Everything above it talks to
the `HttpClient` interface, and every failure arrives as an `ApiError` with a `kind`
(`validation` / `notFound` / `conflict` / `network` / `unexpected`) mapped from the
status codes the API's `BaseController` produces.

`HttpCrudRepository` implements the five-verb shape every resource shares; each
service supplies only its base path. Invoice lines are a sub-resource, so
`InvoiceLineServiceFactory.forInvoice(id)` binds the parent id and returns a
repository with the same interface as the top-level ones.

### Behaviour worth knowing

- Update contracts carry no `id`: the API treats the route id as authoritative.
- Invoice lines are created and edited from the invoice detail page. Leaving unit
  price or tax rate empty sends `null`, which tells the API to snapshot the product's
  current values.
- Creating an invoice requires at least one line; the API rejects an empty array.
- After any line change the invoice header is re-read, because the totals are derived
  server side.
- Lists page with `page` / `pageSize`. The API returns a bare array with no total
  count, so "next page" is inferred from a full page of results.

## PDF generation

Not implemented. `src/pdf/InvoicePdfGenerator.ts` defines the port and ships a
null-object implementation that reports itself unavailable and throws if invoked, so
the button, its wiring and its error handling are all real and exercised.

To supply a real one, implement the interface and change the single line in
`src/core/container.ts` that constructs `NotImplementedInvoicePdfGenerator`. No view
or service needs to change.
