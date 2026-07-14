# CMS

Full-stack CMS scaffolded from the database schema in `./database`, following `./spec/code-gen.convention.md`.

- **Backend** — `src/CMS.API` (.NET 9 Web API, Dapper, Swagger) + `src/CMS.API.Tests` (xUnit)
- **Frontend** — `src/CMS.NG` (Angular 20 standalone, PrimeNG)

First feature implemented end-to-end: **CRUD AppRole** (角色) under 系統管理 Admin.

## Prerequisites

- .NET 9 SDK
- Node 20+ and Angular CLI 20
- SQL Server (`.\SQLEXPRESS`) with the `CMS` database and the `auth.sql` tables

## Backend — CMS.API

```bash
cd src/CMS.API
dotnet run
```

- Runs on http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- CORS allows any `localhost` / `127.0.0.1` origin
- Connection string is in `appsettings.json` (`ConnectionStrings:CMS`)

### AppRole endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/api/app-roles` | All roles (with user count) |
| POST   | `/api/app-roles/query` | Filtered search (keyword, permission-level range) |
| GET    | `/api/app-roles/{id}` | Single role incl. assigned `userIds` |
| POST   | `/api/app-roles` | Create (409 on duplicate RoleId) |
| PUT    | `/api/app-roles` | Update (RoleId in body) |
| DELETE | `/api/app-roles/{id}` | Delete |
| GET    | `/api/lookups/app-users` | Slim AppUser list for the role-users multiselect |

`RoleId` is the string primary key; `pkid` is the identity display code. Role↔User is an
N-N relation via `AppUserRole` (delete-then-reinsert on write).

### Backend tests

```bash
cd src
dotnet test
```

Controller tests boot the real pipeline via `WebApplicationFactory` with in-memory fake
repositories — no SQL Server required. Covers list, filter, view, add (incl. 409/400), and edit.

## Frontend — CMS.NG

```bash
cd src/CMS.NG
npm install   # first time only
npm start     # ng serve on http://localhost:4200
```

- API base URL comes from `src/environments/environment*.ts` (no dev-server proxy).
  - `environment.development.ts` → `http://localhost:5000/api` (used by `ng serve`)
  - `environment.ts` → `/api` (production default)
- Path aliases (see `tsconfig.json`): `@environments/*`, `@app/*`, `@core/*`
- AppRole feature: `src/app/features/app-roles/{app-role-list,app-role-detail,app-role-form}`

### Frontend tests

```bash
cd src/CMS.NG
npm test                                    # Karma + Jasmine (interactive)
ng test --watch=false --browsers=ChromeHeadless   # single run
```

Covers the `AppRoleService` (all endpoints via `HttpTestingController`) and the list, detail,
and form components.
