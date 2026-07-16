# Setup notes

Setup, commands, and one-time procedures for the CMS repo. Enduring conventions live in
[`CLAUDE.md`](../CLAUDE.md); see also `README.md` and `spec/code-gen.convention.md`.

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

## Backend

- CORS allows any localhost origin.

## Backend tests

- `WebApplicationFactory<Program>` + in-memory **fake repositories** (`Fakes/`) swapped in via
  `ConfigureServices` → `RemoveAll` + `AddSingleton`. **No live database is required.**
- Cover list, filter, view, add (incl. 409 duplicate / 400 validation), and edit per feature.
- `Program.cs` ends with `public partial class Program { }` so tests can use
  `WebApplicationFactory<Program>`.

## Frontend tests

- Default Karma + Jasmine setup is retained. Component specs use `provideNoopAnimations`,
  `provideRouter`, and a jasmine-spy service; the service spec uses `HttpTestingController`.

## Adding a new entity

Mirror the AppRole feature as the template.

1. Read its `database/*.sql` and note PK type, FKs, and n-n junction tables.
2. Backend: model trio → repository (+ interface, register in `Program.cs`) → controller;
   add lookup endpoints for any FK targets; add xUnit tests with a fake repo.
3. Frontend: `core/models` + `core/services` → list/detail/form components → route in
   `app.routes.ts` → sidebar entry; add component + service specs.

## Change log

Newest first. Record notable convention or structural changes here.

- **2026-07-16** — Reorganized `CLAUDE.md` into a compact index (~133 → ~54 lines): replaced the
  duplicated Row-audit / Exception-handling prose with a single **Cross-Cutting Conventions** checklist
  (backend + frontend must-follows) that breadcrumbs to the detailed docs, kept a one-paragraph Auth
  summary, and trimmed the exhaustive gstack skill list (skills are auto-discovered). All detail is
  single-sourced in `docs/*`; nothing lost. Reduces always-loaded context.
- **2026-07-16** — Added centralized **exception handling**. Backend `Middleware/ExceptionHandlingMiddleware`
  (registered **first**, right after `Build()`) catches any unhandled exception (controllers, repositories,
  Dapper/SQL), logs full detail **server-side only** via `ILogger`, and returns ONE safe response: HTTP
  **500** `{ "message": "An unexpected error occurred." }` (`GenericMessage`) — no stack/SQL/connection
  detail on the wire (rethrows only if the response already started). Reacts only to *thrown* exceptions, so
  **401/403/validation-400** (set without throwing) are unchanged. Frontend `core/auth/auth.interceptor.ts`
  surfaces `status >= 500` as a friendly **toast** (globally-provided `MessageService`, dedicated
  `GLOBAL_TOAST_KEY = 'global'`, bilingual fallback) without clearing the session; **401** still clears
  session + redirects to `/login`; 403/validation pass through. `App` shell hosts an app-level
  `<p-toast [key]="global">` outside the authenticated `@if`. Tests: `ExceptionHandlingTests`
  (+ `ExceptionHandlingApiFactory` / `ThrowingAppRoleRepository`), `auth.interceptor.spec`. Detail →
  [backend-conventions.md](backend-conventions.md#global-exception-handling) +
  [frontend-conventions.md](frontend-conventions.md#global-error-handling-interceptor).
- **2026-07-16** — **Wired Row audit into all seven CRUD repositories** (PublishStatus, Partner,
  CourseGroup, AppRole, AppUser, Course, FeaturedPromoItem) — completing the writer added 07-15. Each
  mutation now runs in a **transaction** with the audit INSERT on the **same connection/transaction**
  (last statement before `Commit()`), so a rolled-back/failed change leaves no audit row; Update reads the
  row before + after (accurate changed-column list; unchanged update writes none), Delete reads before
  (first string column survives). Each repo has a private `ReadForAuditAsync` selecting **base-table
  columns only** (no FK labels / n-n / counts). Added the **read side**: `IRowAuditRepository` /
  `RowAuditRepository` + `RowAuditController` (`GET /api/rowaudit?tableName={T}&pkid={n}`, newest first,
  empty `tableName` → 400), and the reusable standalone frontend **`RowAuditBadge`**
  (`shared/row-audit-badge`, inputs `tableName` + numeric `pkid`) dropped into the `.page-actions` toolbar
  of every detail + edit page of the six pkid-keyed entities (FeaturedPromoItem has no single-record page).
  Host-page specs add `provideHttpClient()` + `provideHttpClientTesting()`. Tests: `RowAuditRetrofitTests`,
  `RowAuditRepositoryTests`, `RowAuditControllerTests`, `RowAuditBadge` spec. Detail →
  [backend-conventions.md](backend-conventions.md#row-audit-cross-cutting) +
  [frontend-conventions.md](frontend-conventions.md#row-audit-history-badge).
- **2026-07-15** — Added a cross-cutting **Row audit** writer (`Auditing/RowAuditWriter`,
  `IRowAuditWriter`, `AddScoped`) that inserts **one** `RowAudit` row per change to any business table.
  Generic via reflection (`LogInsertAsync`/`LogUpdateAsync`/`LogDeleteAsync<T>`): UserName ← current JWT
  `userName` claim (injected `IHttpContextAccessor`, falls back to `"system"`); PrimaryKeyValues ← the
  `pkid` property; ActionDesc ← first string property (Insert/Delete) or comma-separated changed property
  names (Update, no row when nothing changed), truncated to 1000; DateTime ← now. Reflection logic is in
  pure static helpers so it unit-tests without a DB/HTTP context (`RowAuditWriterTests`). Registered
  `AddHttpContextAccessor()` in `Program.cs`. **Not yet wired into any repository** — that's the next step.
  Detail → [backend-conventions.md](backend-conventions.md#row-audit-cross-cutting).
- **2026-07-15** — Moved the full **Auth (JWT)** section out of `CLAUDE.md` into
  [auth.md](auth.md); `CLAUDE.md` now keeps a short auth summary + a pointer in the reference
  index. Reduces always-loaded context.
- **2026-07-15** — Added **Reset Password to Default** (Admin-only) on the AppUser edit form. Backend
  `POST /api/Auth/reset-password` (body `ResetPasswordRequest { UserId }` — target only): since the
  class-level `[AllowAnonymous]` on `AuthController` bypasses a declarative `[Authorize]` on the action,
  the action enforces auth + role **explicitly by the JWT role claim** — anonymous → 401, non-Admin → 403
  (`Forbid()`), Admin → reset. `IAuthRepository.ResetPasswordToDefaultAsync` reads
  `SysConfig['appConfig'].defaultPassword` at runtime, sets `PasswordHash = SHA256(default)` + stamps
  `PasswordUpdatedTime` (404 if the user is missing); no password/hash ever crosses the wire. Replaces the
  earlier unprotected `POST /api/app-users/{id}/reset-password` (endpoint + `IAppUserRepository.ResetPasswordAsync`
  removed). Frontend: `AppUserService.resetPassword` now POSTs `{ userId }` to `/Auth/reset-password`; the
  edit form (`app-user-form`) shows a confirm-guarded **重設密碼** button only in edit mode when
  `auth.isAdmin()` (moved off the detail page). Tests: `ResetPasswordEndpointTests` (401/403/200, hash =
  SHA256(default), no leak), plus service/form/detail Karma specs.
- **2026-07-15** — Added **Change Password** to My Profile. Backend `POST /api/Auth/change-password`
  (`[Authorize]`, account from the JWT `userId` claim, never the body): verifies `CurrentPassword`
  against the stored `PasswordHash`, enforces `Auth/PasswordPolicy` (len ≥ 8 **and** ≥ 3 of
  upper/lower/digit/symbol, bilingual `ComplexityMessage`), requires new == confirm, then
  `IAuthRepository.UpdatePasswordAsync` sets `PasswordHash = SHA256(new)` + stamps
  `PasswordUpdatedTime`; no hash ever crosses the wire. Frontend adds a Change Password form on the
  profile page validated by a shared `core/auth/password-policy.ts` (mirrors the server rule); server
  rejections shown inline. Tests: `ChangePasswordEndpointTests` (fresh factory per test),
  `password-policy.spec`, and password-form cases in `profile.spec`.
- **2026-07-15** — Course **list inline editing**: double-click a cell to edit (single-click does
  not); type-matched editors (text/number/`p-datepicker`/`p-select` for 上架狀態/`p-checkbox` for
  允許重聽). The three read-only columns — 主代碼 (pkid), 原廠, 課程群組 (FK labels) — stay
  display-only. Commit on blur (select/checkbox on change) → validate (required not cleared,
  非負 numbers, valid dates, 上架日期 ≤ 下架日期) → `getById` then `update` so the N-N lists the
  list row omits are not wiped; invalid values keep the cell in edit mode with an inline error, a
  failed save reverts. New `shared/autofocus` directive; inline-edit unit tests on the list.
- **2026-07-15** — Course detail **QR code**: reusable `shared/qr-code` (`QrCode`) component +
  `QrCodeService` wrapping the new `qrcode` dependency — encodes
  `https://www.uuu.com.tw/Course/Show/{pkid}/{CourseId}`, shows CourseId as the title, downloads a
  PNG. Placed in 基本資料; Karma tests for encode/title/download.
- **2026-07-15** — Added the **FeaturedPromoItem 上稿作業** custom feature (spec
  `spec/custom/FeaturedPromoItem`). Backend: model trio + repo/controller at
  `/api/featured-promo-items` (int IDENTITY PK, INNER-JOINed `PromoCode` (Promotion2) and
  `TrainingCenter.Name` labels; query filters by `TrainingCenter_pkid` + a Monday–Sunday
  `ScheduleOn` range), plus `training-centers` / `promotions` lookups. Frontend: a weekly
  scheduler (`features/featured-promo-items`) with TrainingCenter tabs, a Monday→Sunday week
  navigator, and an inline Edit/New/Paste form whose PromoCode autocomplete resolves
  `Promotion_pkid`; menu entry under 首頁管理 Home. xUnit + Karma tests both sides.
- **2026-07-15** — Trimmed `CLAUDE.md` under 2 KB; moved this change log and one-time
  setup context here, leaving a one-line reference link in `CLAUDE.md`.
- **2026-07-15** — Split detailed conventions out of `CLAUDE.md` into
  [backend-conventions.md](backend-conventions.md) and
  [frontend-conventions.md](frontend-conventions.md); `CLAUDE.md` now holds core rules +
  a pointer index. Reduces always-loaded context.
- **2026-07-15** — Scaffolded reference entities beyond AppRole: PublishStatus, Partner,
  CourseGroup, Course, and AppUser (models, repositories, controllers, xUnit tests + fakes).
- **2026-07-15** — Added the `DateOnly` Dapper type handler (`Data/DateOnlyTypeHandler.cs`,
  registered first in `Program.cs`) with `DateOnlyMappingTests` regression guard, for
  `Course.ScheduleOn`/`ScheduleOff` `date` columns.
