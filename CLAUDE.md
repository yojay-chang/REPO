# CLAUDE.md

CMS scaffolded from SQL Server schemas in `database/*.sql`.

- **Backend** — `src/CMS.API` (.NET 9, Dapper, Swagger) + `CMS.API.Tests` (xUnit).
- **Frontend** — `src/CMS.NG` (Angular 20 standalone + signals, PrimeNG 20 pinned).

## Docs — read before building/modifying (keep them updated)

- Commands, setup, adding an entity, changelog → [docs/setup-notes.md](docs/setup-notes.md)
- Backend core + recipes (PK variants, N-N, FK labels, `date` handler, **row audit**, **exception middleware**) → [docs/backend-conventions.md](docs/backend-conventions.md)
- Frontend core + recipes (widgets, inline-edit, QR, sticky toolbar, nav, **row-audit badge**, **error interceptor**) → [docs/frontend-conventions.md](docs/frontend-conventions.md)
- Auth — JWT login/profile/change-password/admin reset, **forced first-login password change**, guards, interceptor, tests → [docs/auth.md](docs/auth.md)
- Per-entity specs → `spec/**/{Entity}.md`

## Reference features — copy the closest shape

- **PublishStatus** — user-assigned numeric PK; no FK/n-n; simplest.
- **Partner** — IDENTITY PK; FK-target lookup; nav to children.
- **CourseGroup** — like Partner; one `Description`; sorts `pkid DESC`.
- **AppRole** — string PK + `pkid`; n-n users; FK-target lookup.
- **AppUser** — like AppRole plus a bool and a server-managed SHA-256 password (`PasswordHasher`, never sent to frontend); `spec/auth/AppUser`.
- **Course** — richest: `int` IDENTITY; INNER/LEFT FKs; 2×N-N; date/decimal/bool; inline-edit list, QR + sticky toolbar.
- **FeaturedPromoItem** — custom weekly scheduler (not list/detail/form); `spec/custom/FeaturedPromoItem`.

## Cross-Cutting Conventions — every feature MUST follow

Non-optional. This is the checklist; detail + rationale in the docs linked above.

**Row audit — backend (every repository).** Full detail → backend-conventions § Row audit.
- Log via shared `RowAuditWriter` on **every** Insert / Update / Delete (`LogInsert`/`LogUpdate`/`LogDelete`); audit write rides the **same connection + transaction**, so a failed change leaves no audit row.
- **Update** loads the row first → `ActionDesc` = comma-separated **changed column names**. **Insert/Delete** load/read the row → `ActionDesc` = the row's **first string-type column value**.
- `PrimaryKeyValues` = `pkid` as a string; `UserName` = JWT user (fallback `"system"`); **never insert `pkid`** (IDENTITY). Audit reads use **base-table columns only**.

**Row audit — frontend (every detail + form page).** Full detail → frontend-conventions § Row-audit history badge.
- Place `RowAuditBadge` (inputs `tableName`, `pkid`) in the toolbar; shows latest change inline, opens full trail (`GET /api/rowaudit?tableName=&pkid=`). New/unsaved record → `pkid` null, no request.
- Host-page specs add `provideHttpClient()` + `provideHttpClientTesting()`.

**Exceptions — backend.** Full detail → backend-conventions § Global exception handling.
- Global `ExceptionHandlingMiddleware` (registered first) turns any unhandled error into a safe generic **500**, logs full detail **server-side only** — never leak stack/SQL. **Do not** add per-controller try/catch for unexpected errors; leave **401/403/validation-400** untouched (set without throwing).

**Exceptions — frontend.** Full detail → frontend-conventions § Global error handling.
- `auth.interceptor.ts`: `status >= 500` → friendly global toast (no redirect/session-clear); **401** clears session + redirects to `/login`; **403 with `code: "password_change_required"`** → redirect to `/change-password`, session kept (see Auth summary); other 403s + validation pass through. Specs touching the toast/interceptor provide `MessageService`.

## Auth (JWT) — summary

Login + JWT bearer; signing key `symmetricSecurityKey` (≥32 bytes) in `SysConfig['appConfig']`, read at runtime. Global `FallbackPolicy` protects every controller; `AuthController` is `[AllowAnonymous]`. `/api/Auth`: `login`, `profile`, `change-password` (self), `reset-password` (Admin-only via JWT role claim). Passwords SHA-256; no hash on the wire. Frontend keeps `{userId,userName,accessToken}` in session storage, decodes roles/`isAdmin`, guards routes, hides **系統管理 Admin** nav unless admin. **Forced first-login password change**: a stored hash equal to `SHA256(appConfig.defaultPassword)` — a new or Admin-reset account — yields a token carrying the `mustChangePassword` claim; `PasswordChangeRequiredMiddleware` 403s (`code: "password_change_required"`) every `/api` path except login/change-password/profile until `change-password` returns a fresh token. Detail → [docs/auth.md](docs/auth.md).

## gstack

[gstack](https://github.com/garrytan/gstack) installed at `~/.claude/skills/gstack` (skills auto-discovered; see the skills list at session start).

- **Web browsing** — use the **`/browse`** skill for **all** web browsing.
- **Never** use `mcp__claude-in-chrome__*` tools.
