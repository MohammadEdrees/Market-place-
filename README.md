# Market Workplace

An analytics dashboard built as two independent projects:

| Folder     | Stack                                              | Runs on                          |
| ---------- | -------------------------------------------------- | -------------------------------- |
| `frontend/` | Angular 21, PrimeNG 21, Chart.js, PrimeFlex         | http://localhost:4200            |
| `backend/`  | .NET 9 Web API (controllers), in-memory repository | http://localhost:5240            |

API documentation (Swagger UI): **http://localhost:5240/swagger**

## Prerequisites

- Node.js 22.12+ (Node 22.14 verified)
- .NET 9 SDK

## Running

Start each project in its own terminal:

```powershell
# Terminal 1 — API
dotnet run --project backend/MarketWorkplace.Api.csproj

# Terminal 2 — dashboard
cd frontend
npm install        # first time only
npm start
```

Open http://localhost:4200 — you'll be redirected to the **sign-in page**; the dev server
proxies `/api/*` to the API via `frontend/proxy.conf.json`, so no CORS configuration is needed
during development. CORS is nevertheless enabled on the API for `localhost:4200`–`4201` in case
you call it directly.

## Authentication (JWT)

All `/api/dashboard` and `/api/products` endpoints require a bearer token; only
`POST /api/auth/login` is anonymous. The Angular app handles the flow for you: unauthenticated
visits redirect to `/login`, the token is attached to every request by an HTTP interceptor, and
a `401` from the API clears the session and returns to the login page.

**Demo accounts** (also shown on the login page and seeded in `backend/Data/DbInitializer.cs`):

| Email                      | Password     | Role      |
| -------------------------- | ------------ | --------- |
| `superadmin@marketplace.dev` | `123456`   | SuperAdmin |
| `admin@marketplace.dev`    | `Admin123!`  | Admin     |
| `chief.admin@marketplace.dev` | `Chief123!` | Admin    |
| `manager@marketplace.dev`  | `Manager123!` | Manager  |
| `viewer@marketplace.dev`   | `Viewer123!` | Viewer    |

```powershell
# Get a token
Invoke-RestMethod -Method Post -Uri http://localhost:5240/api/auth/login `
  -ContentType 'application/json' `
  -Body '{"email":"admin@marketplace.dev","password":"Admin123!"}'

# Call a protected endpoint with it
Invoke-RestMethod -Uri http://localhost:5240/api/dashboard `
  -Headers @{ Authorization = "Bearer $token" }
```

Configuration lives under the `Jwt` section of `backend/appsettings.json` (issuer, audience,
secret, 8-hour expiry). **Override `Jwt:Secret` per environment** — the checked-in value is a
development placeholder. Tokens are HS256-signed; passwords are stored as PBKDF2 hashes
(`backend/Auth/PasswordHasher.cs`). For role checks use `[Authorize(Roles = "Admin")]` — the
`role` claim is already present in every token.

## API endpoints

| Method   | Route                        | Description                                        |
| -------- | ---------------------------- | -------------------------------------------------- |
| `POST`   | `/api/auth/login`            | Exchange email + password for a JWT (`200`, `401`, `400`) |
| `GET`    | `/api/auth/me`               | Profile behind the current token                   |
| `GET`    | `/api/dashboard`             | Metrics, 12-month revenue trend, category split, recent orders, low-stock count |
| `GET`    | `/api/dashboard/revenue-trend` | Revenue + order count per month                   |
| `GET`    | `/api/products`              | List products (`?search=…&category=…`)             |
| `GET`    | `/api/products/categories`   | Distinct categories                                |
| `GET`    | `/api/products/{id}`         | Single product                                     |
| `POST`   | `/api/products`              | Create (validated, returns `400` with field errors) |
| `PUT`    | `/api/products/{id}`         | Update                                             |
| `DELETE` | `/api/products/{id}`         | Delete (`204`, or `404` when missing)              |

All rows except `POST /api/auth/login` return `401` without a valid bearer token.

Sample requests for an IDE client live in `backend/MarketWorkplace.Api.http`.

## API documentation (Swagger)

- **UI:** http://localhost:5240/swagger — operations grouped into `Auth`, `Dashboard` and
  `Products`, with Try-it-out enabled by default. Click **Authorize** and paste the token from
  `POST /api/auth/login` (Swagger UI adds the `Bearer ` prefix) to call the protected operations.
- **Document:** http://localhost:5240/swagger/v1/swagger.json (OpenAPI 3.0.4).

Descriptions come from XML doc comments: `GenerateDocumentationFile` is enabled in
`backend/MarketWorkplace.Api.csproj`, and `<summary>`/`<param>` comments plus
`[ProducesResponseType]` attributes on the controllers feed the operation and schema docs.

To hide the docs in an environment, set the kill switch in `backend/appsettings.json`:

```json
"Swagger": { "Enabled": false }
```

## Dashboard features

- **Overview** (`/dashboard`) — metric cards with period-over-period deltas, dual-axis
  revenue/orders line chart, sales-by-category doughnut, recent orders table, inventory health.
- **Products** (`/products`) — server-backed CRUD with client-side search, category filter,
  sortable columns, pagination, create/edit dialog, confirm-to-delete, toasts.
- **Sign in** (`/login`) — JWT login with inline errors, guarded routes and a `returnUrl`
  round-trip; the session survives reloads until the token expires.
- Sidebar/topbar shell with the signed-in user's name/role and a sign-out button, light **and**
  dark theme (both persisted in `localStorage`).

## How the data layer works

`backend/Data/MarketDbContext.cs` exposes products, orders and users through EF Core's
in-memory provider (`UseInMemoryDatabase`) — the full `DbContext`/`DbSet` pipeline without a
database server. `backend/Data/DbInitializer.cs` seeds 24 products and ~72 orders across the
last 12 months at startup, so charts and tables have realistic data immediately; data resets
on restart. To make it durable, add the `Microsoft.EntityFrameworkCore.SqlServer` package
and swap `UseInMemoryDatabase("MarketWorkplace")` for `UseSqlServer(connectionString)` in
`Program.cs` — controllers already speak EF Core, so the endpoint contracts stay the same.

Display strings (currency, month names) are formatted with an explicit `en-US` culture in
`DashboardController`, so output does not depend on the machine's regional settings.

## Useful scripts

```powershell
cd frontend
npm start          # dev server with proxy (port 4200)
npm run build      # production build to frontend/dist
```

```powershell
dotnet build backend/MarketWorkplace.Api.csproj   # compile the API
```
