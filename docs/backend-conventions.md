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
