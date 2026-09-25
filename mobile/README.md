# Market Workplace — mobile app

Flutter client (Android + iOS) for the Market Workplace marketplace: browse products and
services, buy/reserve, track orders — and for providers, manage listings, galleries and
order statuses from the phone.

Built with **Riverpod** (state), **go_router** (navigation + auth/role redirects), **dio**
(HTTP), **Material 3**.

## Run

```powershell
flutter pub get
flutter run          # API defaults to http://localhost:5240
```

Point the app at another host with the `API_BASE` define:

```powershell
flutter run --dart-define=API_BASE=http://10.0.2.2:5240        # Android emulator → host
flutter run --dart-define=API_BASE=http://192.168.1.50:5240     # device → host LAN IP
```

Sign in with any mobile account from the seed (e.g. `client@marketplace.dev` / `Client123!`
or `nova@marketplace.dev` / `Nova123!` — the Provider). Registration creates **Client** or
**Provider** accounts; dashboard roles are refused.

## Verify

```powershell
flutter analyze   # 0 issues
flutter test      # unit + widget tests
```

## Layout

```
lib/
  core/        api client (bearer + 401 + problem-details), session store,
               router (redirects/guards), app shell, theme, shared widgets
  features/
    auth/      login, register, session controller
    catalog/   products & services: lists, detail pages, gallery, repository
    orders/    order history + provider status management
    listings/  provider CRUD forms + gallery upload
    profile/   profile edit, avatar, sign out
test/          repository tests (mocked Dio), session, formatters, widget tests
```

See the repository root `README.md` for the full stack (API, dashboard, endpoints).
