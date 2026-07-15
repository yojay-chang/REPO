# CLAUDE.md

CMS scaffolded from SQL Server schemas in `database/*.sql`.

- **Backend** — `src/CMS.API` (.NET 9, Dapper, Swagger) + `CMS.API.Tests` (xUnit).
- **Frontend** — `src/CMS.NG` (Angular 20 standalone + signals, PrimeNG 20 pinned).

**Read before building/modifying an entity** — the detail lives in these; keep them updated:

- Commands, setup, adding an entity, changelog → [docs/setup-notes.md](docs/setup-notes.md)
- Backend core + recipes (PK variants, N-N, FK labels, `date` handler) → [docs/backend-conventions.md](docs/backend-conventions.md)
- Frontend core + recipes (widgets, inline-edit, QR, sticky toolbar, nav) → [docs/frontend-conventions.md](docs/frontend-conventions.md)
- Auth (JWT login/profile/change-password/admin reset, guards, interceptor, tests) → [docs/auth.md](docs/auth.md)
- Per-entity specs → `spec/**/{Entity}.md`

## Reference features — copy the closest shape

- **PublishStatus** — user-assigned numeric PK; no FK/n-n; simplest.
- **Partner** — IDENTITY PK; FK-target lookup; nav to children.
- **CourseGroup** — like Partner; one `Description`; sorts `pkid DESC`.
- **AppRole** — string PK + `pkid`; n-n users; FK-target lookup.
- **AppUser** — like AppRole (string PK + `pkid`, n-n roles) plus a bool and a server-managed SHA-256 password (`PasswordHasher`, never sent to the frontend); `spec/auth/AppUser`.
- **Course** — richest: `int` IDENTITY; INNER/LEFT FKs; 2×N-N; date/decimal/bool; also inline-edit list, QR + sticky toolbar.
- **FeaturedPromoItem** — custom weekly scheduler (not list/detail/form); `spec/custom/FeaturedPromoItem`.

## Auth (JWT, end-to-end)

Login + JWT bearer auth; signing key is `symmetricSecurityKey` (≥32 bytes) in `SysConfig['appConfig']`,
read at runtime. Global `FallbackPolicy` protects every controller; `AuthController` is `[AllowAnonymous]`.
Endpoints under `/api/Auth`: `login`, `profile` (self name), `change-password` (self), `reset-password`
(Admin-only, enforced by JWT role claim). Passwords are SHA-256; no hash ever crosses the wire. Frontend
keeps `{userId,userName,accessToken}` in session storage, decodes roles/`isAdmin` from the token, guards
routes, and hides the **系統管理 Admin** nav unless admin. Full detail (endpoints, files, tests) →
[docs/auth.md](docs/auth.md).
