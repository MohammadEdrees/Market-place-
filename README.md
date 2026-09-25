# Market Workplace

An analytics dashboard built as two independent projects:

| Folder     | Stack                                              | Runs on                          |
| ---------- | -------------------------------------------------- | -------------------------------- |
| `frontend/` | Angular 21, PrimeNG 21, Chart.js, PrimeFlex         | http://localhost:4200            |
| `backend/`  | .NET 9 Web API as a **4-project n-tier solution** (Domain, Application, Infrastructure, Api), EF Core Code First + SQL Server | http://localhost:5240            |

API documentation (Swagger UI): **http://localhost:5240/swagger**

## Prerequisites

- Node.js 22.12+ (Node 22.14 verified)
- .NET 9 SDK
- SQL Server (Developer/Express/LocalDB) — the connection string lives in
  `backend/MarketWorkplace.Api/appsettings.json` (`ConnectionStrings:MarketDb`); the database
  is created automatically on first run.
- Flutter stable (3.47.5 verified) — only needed for the mobile app in `mobile/`.

## Running

Start each project in its own terminal:

```powershell
# Terminal 1 — API
dotnet run --project backend/MarketWorkplace.Api/MarketWorkplace.Api.csproj

# Terminal 2 — dashboard
cd frontend
npm install        # first time only
npm start

# Terminal 3 — mobile app (optional)
cd mobile
flutter pub get    # first time only
flutter run        # with an emulator/device connected
```

Open http://localhost:4200 — you'll be redirected to the **sign-in page**; the dev server
proxies `/api/*` to the API via `frontend/proxy.conf.json`, so no CORS configuration is needed
during development. CORS is nevertheless enabled on the API for `localhost:4200`–`4201` in case
you call it directly. The mobile app talks to the API directly (see
[Mobile app (Flutter)](#mobile-app-flutter) for the base-URL switch).

## Authentication (JWT)

All endpoints except `POST /api/auth/login` and `POST /api/auth/register` require a bearer
token. The Angular app handles the flow for you: unauthenticated
visits redirect to `/login`, the token is attached to every request by an HTTP interceptor, and
a `401` from the API clears the session and returns to the login page.

**Demo accounts** (also shown on the login page and seeded in
`backend/MarketWorkplace.Infrastructure/Data/DbInitializer.cs`):

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

Configuration lives under the `Jwt` section of `backend/MarketWorkplace.Api/appsettings.json`
(issuer, audience, secret, 8-hour expiry). **Override `Jwt:Secret` per environment** — the checked-in value is a
development placeholder. Tokens are HS256-signed; passwords are stored as PBKDF2 hashes
(`backend/MarketWorkplace.Application/Security/PasswordHasher.cs`). The token's `role` claim
carries the caller's role name from the roles table, so `[Authorize(Roles = "Admin")]` and the
shared `Access` rules keep working as before.

## API endpoints

| Method   | Route                        | Description                                        |
| -------- | ---------------------------- | -------------------------------------------------- |
| `POST`   | `/api/auth/login`            | Exchange email + password for a JWT (`200`, `401`, `400`) |
| `POST`   | `/api/auth/register`         | Create a mobile account (Client/Provider) and get a token (`200`, `400`, `409`) |
| `GET`    | `/api/auth/me`               | Profile behind the current token                   |
| `GET`    | `/api/dashboard`             | Metrics, 12-month revenue trend, category split, recent orders, low-stock count |
| `GET`    | `/api/dashboard/revenue-trend` | Revenue + order count per month                   |
| `GET`    | `/api/products`              | Paged products (`?search=…&category=…&sellerId=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/products/categories`   | Managed product category names (filter chips/forms) |
| `GET`    | `/api/products/mine`         | The caller's own product listings                  |
| `GET`    | `/api/products/{id}`         | Single product                                     |
| `POST`   | `/api/products`              | Create (Provider/dashboard admin; validated, `400` with field errors) |
| `PUT`    | `/api/products/{id}`         | Update (owner or dashboard admin)                  |
| `DELETE` | `/api/products/{id}`         | Delete (owner or dashboard admin; `204`/`404`)     |
| `POST`   | `/api/products/{id}/images`  | Upload gallery images (multipart `files`, owner/admin; png/jpg/webp/gif ≤ 5 MB, max 10) |
| `DELETE` | `/api/products/{id}/images/{imageId}` | Remove one gallery image (`204`/`404`)    |
| `GET`    | `/api/services`              | Paged services (`?search=…&category=…&providerId=…&page=…&pageSize=…&sortBy=…&sortDir=…`) |
| `GET`    | `/api/services/categories`   | Managed service category names                     |
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
| `PUT`    | `/api/users/{id}`            | Edit any account — profile, email, role, optional password reset (SuperAdmin/Admin/Manager; `200`/`409`/`400`/`403`/`404`) |
| `POST`   | `/api/users/{id}/image`      | Upload or replace the profile picture (owner or admin; multipart `file`) |
| `DELETE` | `/api/users/{id}/image`      | Remove the profile picture (`204`/`404`)           |
| `GET`    | `/api/roles`                 | All roles with account counts (any signed-in user)  |
| `GET`    | `/api/roles/{id}`            | One role with its account count (`200`/`404`)       |
| `POST`   | `/api/roles`                 | Create a role (SuperAdmin/Admin/Manager; `201`/`400`/`409`/`403`) |
| `PUT`    | `/api/roles/{id}`            | Rename/re-describe a role (SuperAdmin/Admin/Manager; `200`/`400`/`404`/`409`/`403`) |
| `DELETE` | `/api/roles/{id}`            | Delete an unused role (`204`/`404`; `409` while accounts still hold it; `403`) |
| `GET`    | `/api/categories`            | Managed categories with live listing counts (`?kind=Product|Service`; Product kind first, then name) |
| `GET`    | `/api/categories/{id}`       | One category (`200`/`404`)                         |
| `POST`   | `/api/categories`            | Create a category (admin; `201`/`400`/`409` duplicate per kind/`403`) |
| `PUT`    | `/api/categories/{id}`       | Rename a category — cascades to existing listings (admin; `200`/`400`/`404`/`409`/`403`) |
| `DELETE` | `/api/categories/{id}`       | Delete an unused category (`204`/`404`; `409` while listings still use it; `403`) |
| `GET`    | `/api/advertisements`        | Every slide in display order (`sortOrder`, then id) — the dashboard table |
| `GET`    | `/api/advertisements/active` | Slides live in the mobile slider (enabled and inside their schedule window) |
| `GET`    | `/api/advertisements/{id}`   | One slide (`200`/`404`)                            |
| `POST`   | `/api/advertisements`        | Create a slide (admin; `201`/`400` title + schedule validation/`403`) |
| `PUT`    | `/api/advertisements/{id}`   | Update title/subtitle/target/schedule/order (admin; `200`/`400`/`404`/`403`) |
| `DELETE` | `/api/advertisements/{id}`   | Delete a slide and its banner file (`204`/`404`/`403`) |
| `POST`   | `/api/advertisements/{id}/image` | Upload/replace the banner (admin; multipart `file`, png/jpg/webp/gif ≤ 5 MB; the old file is removed) |

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
  listing, rules in `backend/MarketWorkplace.Application/Common/ImageUpload.cs`) and written
  to `backend/MarketWorkplace.Api/wwwroot/images/{products|services|users}` by
  `backend/MarketWorkplace.Infrastructure/Data/ImageStore.cs`; `app.UseStaticFiles()` serves
  them and the dev proxy also maps `/images/*` (`frontend/proxy.conf.json`).
- **Absolute URLs (`BackendUrl`)** — the public origin comes from the `BackendUrl` setting
  in `backend/MarketWorkplace.Api/appsettings.json` (currently `http://localhost:5240`). Every image path the
  API returns is prefixed with it, e.g. `http://localhost:5240/images/products/a1b2….png`,
  so links work anywhere (mobile clients, emails), not only behind the dev proxy. Paths are
  stored absolute in the database, so set the right origin before the first run.
- **Seeding** — at startup `DbInitializer` generates dependency-free placeholder PNGs
  (`backend/MarketWorkplace.Infrastructure/Data/PlaceholderPng.cs`): two per product and per service (linked as
  `ListingImage` rows) and one avatar per user, so galleries and tables never show broken
  images.
- **Endpoints** — `POST`/`DELETE /api/{products|services}/{id}/images[/{imageId}]` for
  galleries and `POST`/`DELETE /api/users/{id}/image` for avatars (owner or dashboard
  admin). On the dashboard you manage listing galleries from the create/edit dialogs and
  your own picture from *Edit my profile* on the Users page.

## Roles

Accounts are assigned to rows of a **`Roles` table** (`Roles.Id`, unique `Name` ≤ 40 chars,
`Description`); `Users.RoleId` references it with `RESTRICT`. Six roles are seeded before the
users — `SuperAdmin`, `Admin`, `Manager`, `Viewer`, `Provider`, `Client` — and everything
role-related reads from the table:

- **login/JWT** — the token's `role` claim is the caller's `Role.Name`, so renaming a role
  changes what members carry on their next sign-in;
- **validation** — `POST /api/auth/register` only accepts the `Client`/`Provider` rows, and
  admin account provisioning accepts any row in the table;
- **dashboard** — the role dropdowns on the Users page are fed from `GET /api/roles`, and the
  **Roles** page (`/roles`, SuperAdmin/Admin/Manager) lists name, description and account
  count with create/edit dialogs and confirm-to-delete;
- **deletion** — `DELETE /api/roles/{id}` answers `409 Conflict` while any account still holds
  the role (reassign those accounts first); unknown ids answer `404`, duplicate names `409`.

The hardcoded role *names* in `Access` (who may list, who is an admin) are unchanged.

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
and reserve. The rules live in `backend/MarketWorkplace.Application/Common/Access.cs`; order visibility is: admins see all,
everyone else sees orders they placed plus orders for their own listings.

Sample requests for an IDE client live in `backend/MarketWorkplace.Api.http`.

## API documentation (Swagger)

- **UI:** http://localhost:5240/swagger — operations grouped into `Auth`, `Dashboard`, `Orders`,
  `Products`, `Roles`, `Services` and `Users`, with Try-it-out enabled by default. Click **Authorize**
  and paste the token from
  `POST /api/auth/login` (Swagger UI adds the `Bearer ` prefix) to call the protected operations.
- **Document:** http://localhost:5240/swagger/v1/swagger.json (OpenAPI 3.0.4).

Descriptions come from XML doc comments: `GenerateDocumentationFile` is enabled in
`backend/MarketWorkplace.Api/MarketWorkplace.Api.csproj`, and `<summary>`/`<param>` comments plus
`[ProducesResponseType]` attributes on the controllers feed the operation and schema docs.

To hide the docs in an environment, set the kill switch in `backend/MarketWorkplace.Api/appsettings.json`:

```json
"Swagger": { "Enabled": false }
```

## Dashboard features

- **Overview** (`/dashboard`) — metric cards with period-over-period deltas, dual-axis
  revenue/orders line chart, sales-by-category doughnut, recent orders table, inventory health.
- **Products** (`/products`) — server-backed CRUD with **server-side pagination, search,
  category filter, provider filter and column sorting** (PrimeNG lazy loading round-trips
  page/sort/filter to the API), row thumbnails, a create/edit dialog with an **image gallery**
  (multi-upload with local previews, remove, click-to-open), confirm-to-delete, toasts, plus a
  **Seller** column (listing owner) and create/edit actions hidden for roles the API would
  reject with 403. The **provider dropdown** narrows the table to one provider's listings via
  the API's `?sellerId=` parameter.
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
  dialogs, a profile detail dialog showing contact info plus the **products related to that
  provider**, and editing via `PUT /api/users/me` — name/phone/location/bio plus a
  **profile picture** (upload/replace/remove). SuperAdmin/Admin/Manager also get:
  - an **Add user** button backed by `POST /api/users` (email + password + role; the
    platform is derived from the role, and the **role dropdown is fed from `GET /api/roles`**), and
  - an **edit pencil on every row** opening an *Edit user* dialog with email, role, an
    **optional password reset** (blank = keep) and the avatar editor, saved through
    `PUT /api/users/{id}`.
- **Roles** (`/roles`) — SuperAdmin/Admin/Manager manage the named roles accounts are assigned
  to: a table of name, description and account count backed by `GET /api/roles`, with a shared
  create/edit dialog and confirm-to-delete. A role still held by accounts is refused by the
  API (`409`), and the page surfaces the returned message so you can reassign first.
- **Categories** (`/categories`) — the managed category list behind every filter chip and
  form dropdown: a Products/Services switcher (`?kind=`) over a table of name, kind, live
  listing count and creation date, with add/rename dialogs and confirm-to-delete. Renames
  cascade to existing listings server-side; a category still in use is refused (`409`) with
  the returned message shown as a toast. Write actions are shown to admins only.
- **Advertisements** (`/advertisements`) — the slides behind the mobile home slider:
  a table with thumbnail, title, a computed status chip (Live/Scheduled/Expired/Off),
  schedule window and display order, plus create/edit dialog (title, subtitle, target
  `product/{id}`/`service/{id}`/URL, sort order, active flag, start/end with time), inline
  enable/disable toggle, **image upload with local preview** (multipart `file`, replaces the
  previous banner) and confirm-to-delete. `GET /api/advertisements/active` is what the
  Flutter app renders.
- **Search** — every list page (Products, Services, Orders, Users) binds its search box to an
  RxJS pipeline (`valueChanges → debounceTime(300) → map(trim) → distinctUntilChanged →
  switchMap`): keystrokes coalesce into a single backend request, stale responses are
  cancelled and a failed request never breaks the stream.
- **Sign in** (`/login`) — JWT login with inline errors, guarded routes and a `returnUrl`
  round-trip; the session survives reloads until the token expires.
- Sidebar/topbar shell with the signed-in user's name/role and a sign-out button, light **and**
  dark theme (both persisted in `localStorage`).

## Mobile app (Flutter)

`mobile/` is a Flutter app (Android + iOS, Material 3) for the marketplace journey:
**browse → buy/reserve → track orders**, plus everything a provider needs to run their
listings from a phone. It talks to the same .NET API as the dashboard.

- **Stack** — Flutter 3.47 / Dart 3.13, **Riverpod** (state), **go_router** (navigation +
  auth/role redirects), **dio** (HTTP), `shared_preferences` (session persistence),
  `image_picker` (avatars & gallery uploads), `intl` (en-US money/date formatting).
- **Code layout** — `lib/core/` (API client with bearer/401 interceptor and problem-details
  error mapping, session store, router, shell, theme, shared widgets) and
  `lib/features/` (`auth`, `catalog`, `orders`, `listings`, `profile`).

### Features

- **Sign in / register** — JWT session restored on launch and persisted across restarts;
  register as **Client** or **Provider** (dashboard roles are refused with a clear message);
  a `401` anywhere drops the session and returns to the sign-in screen.
- **Browse** — paged product and service lists with search, category chips, infinite scroll,
  pull-to-refresh, and detail pages with a swipeable **image gallery**, en-US prices
  (`$1,234.50`) and stock/status badges.
- **Advertisement slider** — the Products page opens with an auto-rotating, swipeable banner
  carousel fed by `GET /api/advertisements/active` (page dots; `product/{id}` / `service/{id}`
  targets open the matching detail page). It hides itself while loading, when no slide is
  active and on request failures, and falls back to a themed gradient card when a slide has
  no image or its image fails to load.
- **Buy / reserve** — confirmation dialog → `POST /api/orders`; out-of-stock products and
  inactive services are disabled, and a provider sees *Edit listing* instead of *Buy* on
  their own items.
- **Orders** — history with search, kind chips and a status filter; providers open an order
  sheet that offers the allowed status transitions (`Processing/Confirmed/Completed/Cancelled/
  Reserved/Refunded`) for incoming orders they manage.
- **My listings** (providers only, hidden from clients in both the nav and the router) —
  tabbed products/services with create/edit/delete forms that mirror the API's validation
  rules — including a **category dropdown fed by the managed category list** — plus a
  **gallery editor**: pick photos → `multipart` upload → remove.
- **Profile** — view/edit name, phone, location and bio (`PUT /api/users/me`, empty =
  clear), avatar upload/replace/remove, sign out.

### Configuration

The API base URL is a compile-time define, default `http://localhost:5240`:

```powershell
cd mobile
flutter run                                          # simulator/emulator on this machine
flutter run --dart-define=API_BASE=http://10.0.2.2:5240        # Android emulator → host
flutter run --dart-define=API_BASE=http://192.168.1.50:5240     # physical device → host LAN IP
```

Android is configured for plain-HTTP dev traffic (`usesCleartextTraffic`) and iOS allows
local networking in `ios/Runner/Info.plist` — swap both for ATS/HTTPS before a production
release.

### Verification

```powershell
cd mobile
flutter analyze    # 0 issues
flutter test       # 57 tests: repositories (mocked Dio), session store, formatters,
                   # router role tabs, login flow, catalogue + advertisement slider widgets
```

## Architecture (n-tier)

`backend/MarketWorkplace.sln` splits the API into four projects:

| Project            | Contains                                                                                                                                                                            |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `MarketWorkplace.Domain`         | Entities only (`User`, `Product`, `Service`, `Order`, `ListingImage`, `Role`, `Category`, `Advertisement`) — no dependencies.                                    |
| `MarketWorkplace.Application`    | DTOs, application services (`AuthService`, `UsersService`, `ProductsService`, `ServicesService`, `OrdersService`, `DashboardService`, `RolesService`) that return the exact HTTP results the controllers delegate to, repository interfaces, `Access` rules, `PasswordHasher`, `ITokenService`, `IImageStore`. |
| `MarketWorkplace.Infrastructure` | `MarketDbContext`, `DbInitializer` + migrations, repository implementations (`EfRepository<T>`, `UserRepository`, `RoleRepository`, …), `ImageStore`, `PlaceholderPng`, and the `AddInfrastructure()` DI wiring. |
| `MarketWorkplace.Api`           | Thin controllers (attributes, XML docs, ModelState checks, delegation), `TokenService`, Swagger setup, `Program.cs` — the composition root calling `AddApplication()` + `AddInfrastructure()`. |

Controllers depend on Application services; Application depends on Domain and repository
interfaces only (queries compose with LINQ, no EF types); Infrastructure implements those
interfaces over EF Core; only the Api project wires everything together.

## How the data layer works

`MarketDbContext` (`backend/MarketWorkplace.Infrastructure/Data/MarketDbContext.cs`) maps the
model to **SQL Server with EF Core Code First**: the connection string lives in
`backend/MarketWorkplace.Api/appsettings.json`
(`ConnectionStrings:MarketDb`, default `Server=localhost;Database=MarketWorkplace`), and the
schema is generated by the migration under `backend/MarketWorkplace.Infrastructure/Migrations/`
(`DbInitializer` calls `Database.Migrate()` at startup — the database is created on first
run and updated whenever you add a migration with
`dotnet ef migrations add <Name> --project backend/MarketWorkplace.Infrastructure --startup-project backend/MarketWorkplace.Api`).

Data access goes through repositories — a generic `IRepository<T>` (`Query`, `QueryReadOnly`,
`Add`, `Remove`, `SaveChanges`) plus eager-load shapes such as `IUserRepository.WithRole()`
and `IProductRepository.Catalog()`. All repositories of a request share one scoped context,
so a single `SaveChanges` commits the whole unit of work.

The fluent model defines the real relationships: a **product belongs to its provider**
(`Products.SellerId → Users.Id`, `SET NULL` so platform demo items stay valid), a **service
requires its provider** (`Services.ProviderId → Users.Id`, `RESTRICT`), orders reference
their buyer/product/service (`SET NULL` — history survives deletes), gallery images
cascade with their listing, and every account holds a role
(`Users.RoleId → Roles.Id`, `RESTRICT`). Primary keys are app-assigned
(`ValueGeneratedNever`), matching the services' `Max + 1` pattern, and `Users.Email` and
`Roles.Name` carry unique indexes.

`backend/MarketWorkplace.Infrastructure/Data/DbInitializer.cs` then seeds the 6 roles,
24 products (owned by the demo sellers/admin), 6 services, ~74 orders across the last 12
months and 8 users (5 dashboard, 3 mobile) **only when a table is empty** — data persists
across restarts. Display strings (currency, month names) are formatted with an explicit
`en-US` culture in `DashboardService`, so output does not depend on the machine's regional
settings.

## Useful scripts

```powershell
cd frontend
npm start          # dev server with proxy (port 4200)
npm run build      # production build to frontend/dist
```

```powershell
dotnet build backend/MarketWorkplace.sln   # compile the whole backend
```
