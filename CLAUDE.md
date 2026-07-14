# CLAUDE.md

Guidance for working in this repo. See `README.md` for run instructions and `spec/code-gen.convention.md` for the code-generation patterns this project follows.

## Overview

Full-stack CMS generated from SQL Server schemas in `database/*.sql`.

- **Backend** — `src/CMS.API` (.NET 9 Web API, Dapper, Swagger) + `src/CMS.API.Tests` (xUnit)
- **Frontend** — `src/CMS.NG` (Angular 20 standalone, PrimeNG 20)
- First feature: **AppRole** (角色) CRUD — the reference implementation to copy for new entities.

## Commands

Backend (from `src/`):
```bash
dotnet build CMS.slnx            # build solution
dotnet test                      # run xUnit tests (no SQL Server needed)
cd CMS.API && dotnet run         # http://localhost:5000, Swagger at /swagger
```

Frontend (from `src/CMS.NG`):
```bash
npm start                                          # ng serve → http://localhost:4200
ng test --watch=false --browsers=ChromeHeadless    # single-run Karma + Jasmine
ng build --configuration development                # dev build
```

## Backend conventions

- **Data access is Dapper only** (no EF). Repositories take `IDbConnectionFactory` and open a
  fresh `IDbConnection` per call; write operations that touch an n-n table use a transaction.
- **Layout**: `Models/`, `Repositories/` (`I{X}Repository` + `{X}Repository`), `Controllers/`.
  Register each repository in `Program.cs` (`AddScoped`); `IDbConnectionFactory` is a singleton.
- **Model trio per table**: `{Table}` (response, incl. nav/count fields), `{Table}Request`
  (write DTO), `{Table}Query` (search DTO).
- **Routes**: `/api/{table-plural}` — GET (all), POST `/query` (filter), GET `/{id}`, POST,
  PUT (id in body, no route param), DELETE `/{id}`. Lookups live under `/api/lookups/{plural}`.
- **String primary keys**: route param is `{id}` with **no `:int` constraint**; the DB `pkid`
  identity column is a display code only, not the route key. (AppRole's PK is `RoleId`.)
- **N-N relations**: expose a subquery count on the list model and an id-list on the detail
  model; write via delete-then-reinsert inside the create/update transaction.
- CORS allows any `localhost`/`127.0.0.1` origin. Swagger via Swashbuckle 7.2.0.
- `Program.cs` ends with `public partial class Program { }` so tests can use
  `WebApplicationFactory<Program>`.

### Backend tests

- `WebApplicationFactory<Program>` + in-memory **fake repositories** (`Fakes/`) swapped in via
  `ConfigureServices` → `RemoveAll` + `AddSingleton`. **No live database is required.**
- Cover list, filter, view, add (incl. 409 duplicate / 400 validation), and edit per feature.

## Frontend conventions

- **Angular 20 standalone components**, signals for local state. No NgModules.
- **PrimeNG is pinned to v20** — v21 requires Angular 21. Do not bump PrimeNG past 20.x while on
  Angular 20. Theme is Aura via `providePrimeNG` in `app.config.ts`; `MessageService` and
  `ConfirmationService` are provided globally there too.
- **API base URL comes from environment files, not a dev-server proxy**:
  `src/environments/environment.ts` (prod, `/api`) and `environment.development.ts`
  (`http://localhost:5000/api`), swapped by `fileReplacements` in `angular.json`.
- **Path aliases** (`tsconfig.json`): `@environments/*`, `@app/*`, `@core/*`.
- **Feature layout**: `features/{table-plural}/{table}-list`, `-detail`, `-form`. Shared models
  in `core/models`, HTTP services in `core/services`.
- **List pages**: sortable/paginated `p-table`, `p-drawer` filter, and session-storage keys
  `{table}-list-filters` / `-sort` / `-page`.
- **Form pages**: Reactive Forms; `forkJoin` for parallel lookup loads on init; disable the PK
  control in edit mode and read it back with `form.getRawValue()`. Services `encodeURIComponent`
  string ids.
- **Sidebar nav** lives in `app.ts` / `app.html`; add new features under the right nav group.
  Styled after the **Ultima** admin template (`ultima.primeng.org`): a light floating white
  card, uppercase muted section label, rounded menu items, collapsible groups with a rotating
  chevron, and a soft primary-tinted `.active-route` highlight. Colors use Aura theme tokens
  (`var(--p-*)`) so the menu tracks the theme — avoid hard-coded hex in `app.scss`.
- Default Karma + Jasmine test setup is retained. Component specs use `provideNoopAnimations`,
  `provideRouter`, and a jasmine-spy service; the service spec uses `HttpTestingController`.

## Adding a new entity

1. Read its `database/*.sql` and note PK type, FKs, and n-n junction tables.
2. Backend: model trio → repository (+ interface, register in `Program.cs`) → controller;
   add lookup endpoints for any FK targets; add xUnit tests with a fake repo.
3. Frontend: `core/models` + `core/services` → list/detail/form components → route in
   `app.routes.ts` → sidebar entry; add component + service specs.
4. Mirror the AppRole feature as the template.
