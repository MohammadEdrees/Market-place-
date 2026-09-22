# Market Workplace

An analytics dashboard built as two independent projects:

| Folder     | Stack                                              | Runs on                          |
| ---------- | -------------------------------------------------- | -------------------------------- |
| `frontend/` | Angular 21, PrimeNG 21, Chart.js, PrimeFlex         | http://localhost:4200            |
| `backend/`  | .NET 9 Web API (controllers), in-memory repository | http://localhost:5240            |

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

Open http://localhost:4200 — the dev server proxies `/api/*` to the API via
`frontend/proxy.conf.json`, so no CORS configuration is needed during development.
CORS is nevertheless enabled on the API for `localhost:4200`–`4201` in case you call it directly.

## API endpoints

| Method   | Route                        | Description                                        |
| -------- | ---------------------------- | -------------------------------------------------- |
| `GET`    | `/api/dashboard`             | Metrics, 12-month revenue trend, category split, recent orders, low-stock count |
| `GET`    | `/api/dashboard/revenue-trend` | Revenue + order count per month                   |
| `GET`    | `/api/products`              | List products (`?search=…&category=…`)             |
| `GET`    | `/api/products/categories`   | Distinct categories                                |
| `GET`    | `/api/products/{id}`         | Single product                                     |
| `POST`   | `/api/products`              | Create (validated, returns `400` with field errors) |
| `PUT`    | `/api/products/{id}`         | Update                                             |
| `DELETE` | `/api/products/{id}`         | Delete (`204`, or `404` when missing)              |

Sample requests for an IDE client live in `backend/MarketWorkplace.Api.http`.

## Dashboard features

- **Overview** (`/dashboard`) — metric cards with period-over-period deltas, dual-axis
  revenue/orders line chart, sales-by-category doughnut, recent orders table, inventory health.
- **Products** (`/products`) — server-backed CRUD with client-side search, category filter,
  sortable columns, pagination, create/edit dialog, confirm-to-delete, toasts.
- Sidebar/topbar shell, light **and** dark theme (persisted in `localStorage`).

## How the data layer works

`backend/Data/InMemoryStore.cs` is a singleton seeded with 24 products and ~72 orders across
the last 12 months, so charts and tables have realistic data immediately. It resets on restart.
To make it durable, swap the store for EF Core (or another ORM) — controllers only depend on
the `InMemoryStore` abstraction today, so the endpoint contracts stay the same.

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
