# Backend conventions (detailed)

Backend core rules plus entity-shape-specific recipes. The reference-feature index lives in
[`CLAUDE.md`](../CLAUDE.md); read this when building or modifying an entity.

## Core

- **Dapper only**, no EF. Repos take `IDbConnectionFactory` (fresh connection per call; n-n writes
  in a transaction), registered `AddScoped`; the factory is a singleton.
- **Layout**: `Models/`, `Repositories/`, `Controllers/`.
- **Model trio**: `{Table}` (response), `{Table}Request` (write), `{Table}Query` (search).
- **Routes** `/api/{table-plural}`: GET all · POST `/query` · GET/POST/PUT (id in body)/DELETE `/{id}`.
- **FK-target lookup**: `/api/lookups/{plural}` returns a slim `{X}Lookup` (PK + display column).

## Primary-key variants

- **String PKs** (e.g. `AppRole.RoleId`): route param `{id}`, **no `:int` constraint**; the `pkid`
  identity is a display code.
- **User-assigned PKs** (e.g. `PublishStatus.pkid` `tinyint`→`byte`, not IDENTITY): `pkid` is in the
  Request; POST checks `ExistsAsync` → 409 on duplicate; INSERT writes `pkid` (no `SCOPE_IDENTITY()`);
  route `{id}` binds the primitive (no `:int` constraint). Form disables the PK control in edit,
  reads via `getRawValue()`. Numeric PK ⇒ no `encodeURIComponent` in the service.
- **System-assigned IDENTITY PKs** (e.g. `Partner.pkid` `smallint`→`short`, IDENTITY): `pkid` is in
  the Request but **ignored on INSERT** — INSERT omits it and returns `SELECT CAST(SCOPE_IDENTITY()
  AS smallint)`; `CreateAsync` returns the new `short`. **No `ExistsAsync`/409 check** (DB assigns
  the id). Route `{id}` binds the primitive. Form has **no PK control** — create sends `pkid: 0`;
  edit shows pkid read-only and matches it on UPDATE. Numeric PK ⇒ no `encodeURIComponent`.

## N-N junctions

- Subquery count on list model, id-list on detail model; write via delete-then-reinsert.
- Multiple junctions on one entity sync inside a single create/update transaction (Course:
  `CourseInCertification` + `CourseJobCategories`).

## Outbound FKs (FK-consumer, e.g. `Course`)

- Alias each `{X}_pkid AS XPkid`, and JOIN the parent's display column in as a **flat read-only
  label** (`p.Name AS PartnerName`, `cg.Description AS CourseGroupDescription`) — single-type
  `QueryAsync<{Table}>`, **no Dapper multi-map / `splitOn`**.
- INNER JOIN for NOT NULL FKs, **LEFT JOIN for nullable** FKs (null label when unset).
- Labels live on the response model only, excluded from Request/INSERT/UPDATE.

## Row audit (cross-cutting)

`Auditing/RowAuditWriter` (`IRowAuditWriter`, `AddScoped`) writes **one** `RowAudit` row per change to
any business table. It is **generic** — the primary key and description columns are found by reflection,
so no per-entity wiring. Each log method has two overloads:

- Stand-alone: `LogInsertAsync<T>(tableName, entity)` · `LogUpdateAsync<T>(tableName, before, after)` ·
  `LogDeleteAsync<T>(tableName, entity)` — opens its own connection.
- **Same-connection/transaction**: the same three names prefixed with `(IDbConnection, IDbTransaction?, …)`
  — the audit INSERT rides the caller's connection/transaction so a rolled-back or failed change leaves
  no audit row. **This is the form the repositories use.**

**Wired into all seven CRUD repositories** — PublishStatus, Partner, CourseGroup (Lab 03 simple),
AppRole, AppUser, Course (Lab 03 n-n) and FeaturedPromoItem (Lab 04). The retrofit convention per op:

- Every mutating method now runs inside a **transaction** (the simple repos gained one; the n-n repos
  already had one), and the audit call is the last statement **before `Commit()`**.
- **Insert** — after the row exists (its pkid known), read it back and `LogInsert`.
- **Update** — read the row **before**, apply the update, read it **after**, `LogUpdate(before, after)`
  so the changed-column list is accurate; an update that changes nothing writes no row (`BuildUpdate`
  returns null). A missing row rolls back and returns `false` (no audit).
- **Delete** — read the row **before** deleting, then `LogDelete` (so the first string column survives).
- Each repo has a private `ReadForAuditAsync(db, tx, pk)` that selects **base-table columns only** — no
  JOINed FK labels, no n-n lists, no `COUNT(*)` subqueries — so the changed-column diff and the
  first-string `ActionDesc` reflect only real columns of that table (e.g. `Course` excludes `PartnerName`;
  `AppRole`/`AppUser` exclude the user/role counts; `AppUser` never reads `PasswordHash`).
- `TableName` is the real DB table name, held as a `private const string TableName` on each repo.

Coverage: `RowAuditWriterTests` unit-tests the pure builders; `RowAuditRetrofitTests` drives the real
`PublishStatusRepository` against an in-memory SQLite DB and asserts Insert/Update/Delete each write the
right row, an unchanged update writes none, and a failed insert (duplicate PK) leaves no audit row.

Column mapping (Dapper INSERT; `pkid` is IDENTITY, never inserted — bracket `[DateTime]`, a reserved word):

- **UserName** — the `userName` claim of the current request's JWT (via injected `IHttpContextAccessor`;
  falls back to `ClaimTypes.Name`, then the literal `"system"` when unauthenticated).
- **PrimaryKeyValues** — the entity's `pkid` property (case-insensitive match) as a string.
- **ActionType** — `"Insert"` | `"Update"` | `"Delete"`.
- **ActionDesc** — Insert/Delete: the **first string property in declaration order** (Title/Name/Code…).
  Update: a comma-separated list of the property **names** whose value differs between `before`/`after`;
  when nothing changed `BuildUpdate` returns `null` and **no row is written**. Truncated to **1000** chars.
- **DateTime** — `DateTime.Now`.

The reflection logic lives in **pure static** helpers (`BuildInsert`/`BuildUpdate`/`BuildDelete`,
`GetPrimaryKeyValue`, `GetFirstStringPropertyValue`, `GetChangedPropertyNames`, `ResolveUserName`) that
take the timestamp + user name as arguments — so they unit-test with no DB and no HTTP context
(`RowAuditWriterTests`). Requires `builder.Services.AddHttpContextAccessor()` in `Program.cs`.
Declaration-order/changed-name detection relies on `Type.GetProperties()` order, which holds for the flat
POCO models here (not a documented CLR guarantee for inherited types).

**Reading the trail.** `IRowAuditRepository.GetForRecordAsync(tableName, pkid)` (`RowAuditRepository`,
`AddScoped`) returns the entries for one record as `RowAuditEntry` (DateTime / UserName / ActionType /
ActionDesc), matched on `TableName` + `PrimaryKeyValues = pkid.ToString()` and ordered `[DateTime] DESC,
pkid DESC` (newest first, audit `pkid` as the tie-breaker). Exposed by `RowAuditController` as
`GET /api/rowaudit?tableName={T}&pkid={n}` (empty `tableName` → 400); protected by the global auth policy
like every other controller. The frontend `RowAuditBadge` consumes it — see frontend-conventions. Coverage:
`RowAuditRepositoryTests` (real repo on SQLite: filter + newest-first) and `RowAuditControllerTests`
(endpoint via `WebApplicationFactory` + `FakeRowAuditRepository`).

## Global exception handling

`Middleware/ExceptionHandlingMiddleware` catches **any** unhandled exception thrown downstream
(controllers, repositories, Dapper/SQL) and turns it into ONE consistent error response: HTTP **500**
with a generic JSON body `{ "message": "An unexpected error occurred." }`
(`ExceptionHandlingMiddleware.GenericMessage`). The full exception (message + stack trace) is logged
server-side only via `ILogger` — the **stack trace, SQL text, and connection details never reach the
client**. Registered **first** in the pipeline (`app.UseMiddleware<ExceptionHandlingMiddleware>()`
immediately after `Build()`, before Swagger/CORS/auth) so it wraps everything.

- It only reacts to *thrown* exceptions, so the status codes that are set **without throwing** flow
  through untouched: **401** (unauthenticated, from the auth middleware), **403** (forbidden,
  `Forbid()`), and **validation/400** (model binding / `BadRequest`). These are unchanged by design.
- If `Response.HasStarted` it rethrows (can't rewrite a response already streaming).
- Tests: `ExceptionHandlingTests` (+ `ExceptionHandlingApiFactory`, which keeps real auth but swaps the
  AppRole repo for `ThrowingAppRoleRepository`) prove a throwing endpoint → 500 + generic message with
  no leaked stack trace/SQL, while 401/403/validation-400 are unchanged.

## `date`/`time` columns (`DateOnly`/`TimeOnly`)

Dapper (2.1.66 + Microsoft.Data.SqlClient 6.0.1) does **not** map these natively — it hands back
`DateTime` and the cast fails at runtime (`Invalid cast from 'System.DateTime' to
'System.DateOnly'`). Register a type handler at app startup:
`SqlMapper.AddTypeHandler(new DateOnlyTypeHandler())` in `Program.cs` (see
`Data/DateOnlyTypeHandler.cs`) — it is the **first statement**, before `CreateBuilder`.

- **Why order matters**: Dapper compiles and **caches one deserializer per result type**, and the
  read path only routes a column through the handler if the handler is present in `typeHandlers`
  **at the moment that deserializer is first built** (it checks `typeHandlers.ContainsKey(type)`;
  the handler does *not* need to be in `typeMap`). Register after the first query for a type and
  Dapper caches a handler-less deserializer that keeps throwing for the life of the process.
- **Operational gotcha**: `dotnet watch` hot-reload does **not** re-run `Program`'s top-level
  statements, so adding/moving the registration under an active watch session leaves the live
  process unregistered → the cast error persists. **Fully restart** the API (stop + `dotnet run`),
  don't rely on hot-reload, after touching the handler or its registration.
- Regression guard: `CMS.API.Tests/DateOnlyMappingTests.cs` boots the real `Program` and asserts a
  `date`→`DateOnly` read works; it fails with the production error if the registration is removed.
- Only `date` columns exist so far (`Course`/`Promotion2` `ScheduleOn`/`ScheduleOff`); no `time`
  column ⇒ no `TimeOnly` handler yet. Add a `TimeOnlyTypeHandler` the same way if one appears.
