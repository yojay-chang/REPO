# CLAUDE.md

CMS scaffolded from SQL Server schemas in `database/*.sql`.

- **Backend** — `src/CMS.API` (.NET 9, Dapper, Swagger) + `CMS.API.Tests` (xUnit).
- **Frontend** — `src/CMS.NG` (Angular 20 standalone + signals, PrimeNG 20 pinned).

## Docs — read the relevant one before building/modifying (keep them updated)

- Commands, setup, adding an entity, **reference features to copy**, changelog → [setup-notes.md](docs/setup-notes.md)
- Backend recipes: PK variants, N-N, FK labels, `date` handler, **row audit**, **exception middleware** → [backend-conventions.md](docs/backend-conventions.md)
- Frontend recipes: widgets, inline-edit, QR, sticky toolbar, nav, **row-audit badge**, **error interceptor**, **print/PDF** → [frontend-conventions.md](docs/frontend-conventions.md)
- Auth: JWT login/profile/change-password/admin reset, **forced first-login password change**, guards, interceptor → [auth.md](docs/auth.md)
- Per-entity specs → `spec/**/{Entity}.md`

## Cross-Cutting Conventions — every feature MUST follow

Non-optional. The **rule** is here; the **how** + rationale is in the linked doc section.

- **Row audit — backend** (every repository): log every Insert/Update/Delete via shared `RowAuditWriter` on the **same connection + transaction**; never insert `pkid` (IDENTITY); audit reads use base-table columns only. → backend-conventions § Row audit.
- **Row audit — frontend** (every detail + form page): put `RowAuditBadge` (`tableName`, `pkid`) in the toolbar; host specs add `provideHttpClient()` + `provideHttpClientTesting()`. → frontend-conventions § Row-audit history badge.
- **Exceptions — backend**: global `ExceptionHandlingMiddleware` (registered first) → safe generic **500**, full detail logged **server-side only**; no per-controller try/catch; leave 401/403/validation-400 untouched. → backend-conventions § Global exception handling.
- **Exceptions — frontend**: `auth.interceptor.ts` — `status >= 500` → friendly global toast (no logout); **401** → clear session + `/login`; **403 with `code: "password_change_required"`** → `/change-password`, session kept; other 403s/validation pass through; specs touching it provide `MessageService`. → frontend-conventions § Global error handling.
- **Print / Save as PDF — frontend**: global `@media print` hides all app chrome (sidebar, header, toast, `.page-actions` toolbar + row-audit badge) on **every** page; the print button is opt-in per page (`window.print()`, CJK renders natively). → frontend-conventions § Print / Save as PDF.

## Auth (JWT)

Login + JWT bearer; signing key `symmetricSecurityKey` (≥32 bytes) in `SysConfig['appConfig']`, read at runtime. Global `FallbackPolicy` guards every controller; `AuthController` is `[AllowAnonymous]`. `/api/Auth`: `login`, `profile`, `change-password` (self), `reset-password` (Admin-only). Passwords SHA-256; no hash on the wire. Frontend stores `{userId,userName,accessToken}` in session storage, decodes roles/`isAdmin`, guards routes, hides **系統管理 Admin** nav unless admin. **Forced first-login password change**: a stored hash equal to `SHA256(appConfig.defaultPassword)` — a new or Admin-reset account — yields a token carrying the `mustChangePassword` claim; `PasswordChangeRequiredMiddleware` 403s (`code: "password_change_required"`) every `/api` path except login/change-password/profile until `change-password` returns a fresh token. → docs/auth.md.

## gstack

[gstack](https://github.com/garrytan/gstack) at `~/.claude/skills/gstack` (skills auto-discovered).
- **Web browsing** — use the **`/browse`** skill for all web browsing.
- **Never** use `mcp__claude-in-chrome__*` tools.
