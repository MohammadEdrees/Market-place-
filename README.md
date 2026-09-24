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

All endpoints except `POST /api/auth/login` and `POST /api/auth/register` require a bearer
token. The Angular app handles the flow for you: unauthenticated
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

Mobile accounts (`type = Mobile`, used by the marketplace endpoints):

| Email                    | Password     | Role     | Notes                                      |
| ------------------------ | ------------ | -------- | ------------------------------------------ |
| `seller@marketplace.dev` | `Seller123!` | Provider | Sam Seller — sells products, offers services |
| `nova@marketplace.dev`   | `Nova123!`   | Provider | Nova Services — on-site installation/repair |
| `client@marketplace.dev` | `Client123!` | Client   | Cody Client — buys products, reserves services |

New mobile users sign up with `POST /api/auth/register` (returns a token immediately);
dashboard accounts are provisioned by an admin.

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
| `POST`   | `/api/auth/register`         | Create a mobile account (Client/Provider) and get a token (`200`, `400`, `409`) |
| `GET`    | `/api/auth/me`               | Profile behind the current token                   |
| `GET`    | `/api/dashboard`             | Metrics, 12-month revenue trend, category split, recent orders, low-stock count |
| `GET`    | `/api/dashboard/revenue-trend` | Revenue + order count per month                   |
| `GET`    | `/api/products`              | Paged products (`?search=…&category=…&sellerId=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/products/categories`   | Distinct categories                                |
| `GET`    | `/api/products/mine`         | The caller's own product listings                  |
| `GET`    | `/api/products/{id}`         | Single product                                     |
| `POST`   | `/api/products`              | Create (Provider/dashboard admin; validated, `400` with field errors) |
| `PUT`    | `/api/products/{id}`         | Update (owner or dashboard admin)                  |
| `DELETE` | `/api/products/{id}`         | Delete (owner or dashboard admin; `204`/`404`)     |
| `POST`   | `/api/products/{id}/images`  | Upload gallery images (multipart `files`, owner/admin; png/jpg/webp/gif ≤ 5 MB, max 10) |
| `DELETE` | `/api/products/{id}/images/{imageId}` | Remove one gallery image (`204`/`404`)    |
| `GET`    | `/api/services`              | Paged services (`?search=…&category=…&providerId=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/services/categories`   | Distinct service categories                        |
| `GET`    | `/api/services/mine`         | The caller's own services                          |
| `GET`    | `/api/services/{id}`         | Single service                                     |
| `POST`   | `/api/services`              | Create a service (cost, contact, location, offers) |
| `PUT`    | `/api/services/{id}`         | Update (owner or dashboard admin)                  |
| `DELETE` | `/api/services/{id}`         | Delete (owner or dashboard admin)                  |
| `POST`   | `/api/services/{id}/images`  | Upload gallery images (multipart `files`, owner/admin; same rules as products) |
| `DELETE` | `/api/services/{id}/images/{imageId}` | Remove one gallery image (`204`/`404`)    |
| `POST`   | `/api/orders`                | Buy a product (`kind=Product`) or reserve a service (`kind=Service`) |
| `GET`    | `/api/orders`                | Paged orders visible to the caller (all for dashboard admins; `?search=…&kind=…&status=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/orders/mine`           | Orders the caller placed                           |
| `GET`    | `/api/orders/{id}`           | Single order                                       |
| `PUT`    | `/api/orders/{id}/status`    | Status transition (Processing/Confirmed/Completed/Cancelled/Reserved/Refunded) |
| `GET`    | `/api/users`                 | Paged profiles — admins see everyone, others see providers (`?search=…&role=…&type=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/users/{id}`            | One profile incl. contact info (phone/location/bio) |
| `PUT`    | `/api/users/me`              | Update your own name/contact fields                |
| `POST`   | `/api/users`                 | Add a dashboard/mobile account (SuperAdmin/Admin/Manager; `201`/`409`/`400`/`403`) |
| `POST`   | `/api/users/{id}/image`      | Upload or replace the profile picture (owner or admin; multipart `file`) |
| `DELETE` | `/api/users/{id}/image`      | Remove the profile picture (`204`/`404`)           |

All rows except `POST /api/auth/login` and `POST /api/auth/register` return `401` without a
valid bearer token; listing and management operations additionally enforce the role rules
below and return `403` when they do not apply.

The four list endpoints (`/api/products`, `/api/services`, `/api/orders`, `/api/users`) do
their **pagination, filtering and sorting on the server** and return a paged envelope:

```json
{ "items": [ … ], "total": 24, "page": 1, "pageSize": 20 }
```

`page` is 1-based, `pageSize` is clamped to 1–100 (default 20), and `sortBy` accepts only a
per-endpoint whitelist (e.g. `name`/`price`/`stock` for products, `date`/`total`/`status`
for orders) with `sortDir=asc|desc`. The dashboard tables drive this via PrimeNG's lazy
loading, so page, sort and filter changes always round-trip to the API.

## Images (galleries & avatars)

Products and services carry a **gallery** (multiple images); every user has a single
**profile picture**:

- **Storage** — uploads are validated (png/jpg/jpeg/webp/gif, ≤ 5 MB, max 10 images per
  listing) and written to `backend/wwwroot/images/{products|services|users}` by
  `backend/Data/ImageStore.cs`; `app.UseStaticFiles()` serves them and the dev proxy also
  maps `/images/*` (`frontend/proxy.conf.json`).
- **Absolute URLs (`BackendUrl`)** — the public origin comes from the `BackendUrl` setting
  in `backend/appsettings.json` (currently `http://localhost:5240`). Every image path the
  API returns is prefixed with it, e.g. `http://localhost:5240/images/products/a1b2….png`,
  so links work anywhere (mobile clients, emails), not only behind the dev proxy. Change the
  setting if the API is exposed elsewhere — the in-memory store reseeds itself on restart,
  so stored paths pick up the new origin immediately.
- **Seeding** — at startup `DbInitializer` generates dependency-free placeholder PNGs
  (`backend/Data/PlaceholderPng.cs`): two per product and per service (linked as
  `ListingImage` rows) and one avatar per user, so galleries and tables never show broken
  images.
- **Endpoints** — `POST`/`DELETE /api/{products|services}/{id}/images[/{imageId}]` for
  galleries and `POST`/`DELETE /api/users/{id}/image` for avatars (owner or dashboard
  admin). On the dashboard you manage listing galleries from the create/edit dialogs and
  your own picture from *Edit my profile* on the Users page.

## Marketplace (mobile users)

The API serves two platforms: the **web dashboard** (admin/back-office users, `type = Dashboard`)
and **mobile clients** (`type = Mobile`) with two roles:

- **Provider** (service provider / seller) — adds products and services (cost, contact info,
  location, offers), edits their **own** listings, and manages status on orders for their items.
- **Client** — browses the catalogue, views seller/provider profiles and contact details,
  **buys products** (`POST /api/orders` with `kind=Product`, decrements stock / increments sold)
  and **reserves services** (`kind=Service`, order starts as `Reserved`).

Who may **add** products/services: mobile **Providers** and dashboard **SuperAdmin / Admin /
Manager**. Dashboard **Viewer**s and mobile **Client**s get `403` — they may only browse, buy
and reserve. The rules live in `backend/Auth/Access.cs`; order visibility is: admins see all,
everyone else sees orders they placed plus orders for their own listings.

Sample requests for an IDE client live in `backend/MarketWorkplace.Api.http`.

## API documentation (Swagger)

- **UI:** http://localhost:5240/swagger — operations grouped into `Auth`, `Dashboard`, `Orders`,
  `Products`, `Services` and `Users`, with Try-it-out enabled by default. Click **Authorize**
  and paste the token from
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
- **Products** (`/products`) — server-backed CRUD with **server-side pagination, search,
  category filter and column sorting** (PrimeNG lazy loading round-trips page/sort/filter to
  the API), row thumbnails, a create/edit dialog with an **image gallery** (multi-upload with
  local previews, remove, click-to-open), confirm-to-delete, toasts, plus a **Seller**
  column (listing owner) and create/edit actions hidden for roles the API would reject with 403.
- **Services** (`/services`) — marketplace listings with server-side search/category filter,
  a paginated, sortable table with thumbnails (cost, provider, location, offers) and a full
  create/edit dialog (title, description, category, cost, contact info, location, offers)
  that manages the listing gallery just like Products.
- **Orders & reservations** (`/orders`) — product purchases and service reservations with
  server-side search + kind/status filters on a paginated table, kind/status tags, and a
  status-transition dialog
  (Processing/Confirmed/Completed/Cancelled/Reserved/Refunded).
- **Users** (`/users`) — profile directory (roles, platform, phone, location) with
  server-side search and role/platform filters on a paginated table, **avatars** in rows and
  dialogs, a profile detail dialog showing contact info, and self-service editing of your own
  profile — name/phone/location/bio plus a **profile picture** (upload/replace/remove) via
  `PUT /api/users/me` and `POST`/`DELETE /api/users/{id}/image`. SuperAdmin/Admin/Manager
  also get an **Add user** button backed by `POST /api/users` (email + password + role; the
  platform is derived from the role).
- **Search** — every list page (Products, Services, Orders, Users) binds its search box to an
  RxJS pipeline (`valueChanges → debounceTime(300) → map(trim) → distinctUntilChanged →
  switchMap`): keystrokes coalesce into a single backend request, stale responses are
  cancelled and a failed request never breaks the stream.
- **Sign in** (`/login`) — JWT login with inline errors, guarded routes and a `returnUrl`
  round-trip; the session survives reloads until the token expires.
- Sidebar/topbar shell with the signed-in user's name/role and a sign-out button, light **and**
  dark theme (both persisted in `localStorage`).

## How the data layer works

`backend/Data/MarketDbContext.cs` exposes products, services, orders and users through EF Core's
in-memory provider (`UseInMemoryDatabase`) — the full `DbContext`/`DbSet` pipeline without a
database server. `backend/Data/DbInitializer.cs` seeds 24 products (owned by the demo
sellers/admin), 6 services, ~74 orders across the last 12 months and 8 users (5 dashboard,
3 mobile) at startup, so charts and tables have realistic data immediately; data resets
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
