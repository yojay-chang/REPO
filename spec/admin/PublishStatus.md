# Build Spec for PublishStatus

- database schema: `.\database\admin.sql`

`PublishStatus` is a small status/enum lookup table describing the publish lifecycle
of content entities (draft / published / discontinued). It is referenced as a foreign
key by `Course` (`Course.PublishStatus_pkid`) and `Promotion2` (`Promotion2.PublishStatus_pkid`).
Its primary key `pkid` is a **user-assigned `tinyint`** (NOT an IDENTITY column) — the
code is entered by the operator on create and is immutable thereafter (the same shape as
`AppRole.RoleId`, but numeric).

---

## Summary

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **`tinyint` (byte), user-assigned — NOT IDENTITY**. Entered on create, immutable on edit. |
| Foreign Keys | None |
| Required Fields | `pkid`, `Description`, `IsDraft`, `IsPublished`, `IsDiscontinued` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course` (`Course.PublishStatus_pkid`), `Promotion2` (`Promotion2.PublishStatus_pkid`) — see note below |
| Query Filters | keyword (LIKE on `Description`); tri-state bool toggles on `IsDraft`, `IsPublished`, `IsDiscontinued` |
| Default Sort | `pkid ASC` |

---

## Localization

### Chinese Table Name

- PublishStatus: 發布狀態
- Description: 內容發布狀態代碼（草稿／已發布／已停用）

### Chinese Column Names

- pkid: 主代碼
- Description: 狀態說明
- IsDraft: 草稿
- IsPublished: 已發布
- IsDiscontinued: 已停用

---

## Required Fields

Required (NOT NULL):

- `pkid` (byte) — user-supplied on create; range 0–255; disabled in edit mode
- `Description` (nvarchar 50)
- `IsDraft` (bit)
- `IsPublished` (bit)
- `IsDiscontinued` (bit)

Optional (nullable): none — every column is NOT NULL.

The three `bit` flags default to `false` on the new form.

---

## Foreign Keys

**N/A** — `PublishStatus` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no outbound FK columns.

---

## Primary-Foreign Links

Two tables reference `PublishStatus.pkid` as an FK target:

- **Course** (`Course.PublishStatus_pkid`) — 對應課程
- **Promotion2** (`Promotion2.PublishStatus_pkid`) — 對應活動

**Build note:** the `Course` and `Promotion2` CRUD features do **not exist yet** in this
scaffold (only `AppRole` and `PublishStatus` are built). To avoid dead navigation links,
the list/detail pages will **not** render "查看課程 / 查看活動" buttons in this pass.
When those features are added, wire buttons to `/courses?publishStatusPkid={pkid}` and
`/promotions?publishStatusPkid={pkid}`. Documented here for that future step.

For the same reason no cross-sub-system usage-count subqueries are added to the list model
(they would hard-couple this admin feature to the course/promotion tables).

---

## N-N Relationships

**N/A**

---

## Query Filters

`POST /api/publish-statuses/query` accepts `PublishStatusQuery`:

- **keyword**: `string?` — LIKE on `Description` (the only string column).
- **IsDraft**: `bool?` — tri-state exact match (null = no filter).
- **IsPublished**: `bool?` — tri-state exact match.
- **IsDiscontinued**: `bool?` — tri-state exact match.

`pkid` is numeric and excluded from the keyword search.

---

## Lookup Endpoints Required

`PublishStatus` is itself an FK target (used by `Course`, `Promotion2`), so it needs a
slim lookup endpoint for those future features.

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/publish-statuses` | **New** | `{ pkid, description }[]` ordered by `pkid ASC` |

New lookup model `PublishStatusLookup` (`Pkid` byte, `Description` string).

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/publish-statuses` | List all (ordered `pkid ASC`) |
| `POST` | `/api/publish-statuses/query` | Filtered query (body: `PublishStatusQuery`) |
| `GET` | `/api/publish-statuses/{id}` | Get by pkid |
| `POST` | `/api/publish-statuses` | Create (pkid supplied; 409 on duplicate) |
| `PUT` | `/api/publish-statuses` | Update (pkid from body) |
| `DELETE` | `/api/publish-statuses/{id}` | Delete |
| `GET` | `/api/lookups/publish-statuses` | Slim lookup list (new) |

Route param `{id}` binds to `byte` — no `:int` constraint (consistent with the string-PK
convention of leaving `{id}` unconstrained). No auth attributes.

---

## Backend Notes

### Models

```csharp
// PublishStatus.cs — response model
public class PublishStatus
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// PublishStatusRequest.cs — write DTO (pkid included; it is the user-assigned PK)
public class PublishStatusRequest
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// PublishStatusQuery.cs — search DTO
public class PublishStatusQuery
{
    public string? Keyword { get; set; }
    public bool? IsDraft { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsDiscontinued { get; set; }
}

// PublishStatusLookup.cs — slim FK-target lookup row
public class PublishStatusLookup
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

### SQL — SELECT

```sql
SELECT s.pkid, s.Description, s.IsDraft, s.IsPublished, s.IsDiscontinued
FROM PublishStatus s
-- GetAll / Query: ORDER BY s.pkid ASC
-- GetById:        WHERE s.pkid = @Pkid
```

No aliases or `RTRIM()` needed (`Description` is `nvarchar`, not `nchar`).

### SQL — INSERT

pkid is user-supplied (not IDENTITY), so it is written explicitly; no `SCOPE_IDENTITY()`.

```sql
INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
```

`CreateAsync` returns the supplied `Pkid`. Controller checks `ExistsAsync(pkid)` first → 409.

### SQL — UPDATE

pkid is immutable (WHERE key only):

```sql
UPDATE PublishStatus
   SET Description = @Description,
       IsDraft = @IsDraft,
       IsPublished = @IsPublished,
       IsDiscontinued = @IsDiscontinued
 WHERE pkid = @Pkid;
```

### N-N Sync Pattern

N/A. No transaction needed (single-row writes), matching the simplest repo shape.

### Special Column Notes

- `pkid` is `tinyint` → C# `byte`. It is the PK **and** the display code; there is no
  separate identity column (differs from `AppRole`, where `pkid` is IDENTITY and `RoleId`
  is the string PK).
- No `DateOnly` / `TimeOnly` / `nchar` / computed columns — no type handlers required.

### Repository shape

Mirror `AppRoleRepository` minus the n-n user sync: `GetAllAsync`, `QueryAsync`,
`GetByIdAsync`, `ExistsAsync(byte)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync(byte)`.
Fresh connection per call via `IDbConnectionFactory`; register `AddScoped`.

---

## Frontend Notes

### Angular model (`publish-status.model.ts`)

```ts
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}
export interface PublishStatusRequest {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}
export interface PublishStatusQuery {
  keyword?: string | null;
  isDraft?: boolean | null;
  isPublished?: boolean | null;
  isDiscontinued?: boolean | null;
}
```

### Route table (`app.routes.ts`)

| Path | Component |
|------|-----------|
| `publish-statuses` | `PublishStatusList` |
| `publish-statuses/new` | `PublishStatusForm` |
| `publish-statuses/:id/edit` | `PublishStatusForm` |
| `publish-statuses/:id` | `PublishStatusDetail` |

`/new` registered before `/:id`.

### Service (`publish-status.service.ts`)

Standard six methods. `pkid` is numeric, so `getById`/`delete` interpolate the number
directly (no `encodeURIComponent` needed). Base URL `${environment.apiUrl}/publish-statuses`.

### List component

- Columns: 主代碼 (pkid), 狀態說明 (description), 草稿 (isDraft), 已發布 (isPublished),
  已停用 (isDiscontinued), 操作. Bool columns render a `pi pi-check` / `—` (or a `p-tag`).
- Default sort `pkid ASC`; sortable/paginated `p-table`.
- Filter drawer (`p-drawer`): keyword `pInputText`; three tri-state `p-select`
  (是 / 否 / 全部) for the flags — each `appendTo="body"`.
- Session keys: `publish-status-list-filters`, `publish-status-list-sort`,
  `publish-status-list-page`.
- Delete confirm: ``確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？``

### Detail component

- Read-only grid: 主代碼, 狀態說明, and the three flags shown as 是/否 (or `p-tag`).
- No Primary-Foreign buttons this pass (see Primary-Foreign Links note).

### Form component

- Reactive Forms, sticky header toolbar with 取消 / 儲存.
- `pkid`: `p-inputnumber` (`[min]="0" [max]="255" [useGrouping]="false"`), required.
  **Disabled in edit mode** (`form.controls.pkid.disable()`); read the value via
  `getRawValue()` on save.
- `description`: `pInputText`, required, maxlength 50.
- `isDraft` / `isPublished` / `isDiscontinued`: `p-toggleswitch` (or `p-checkbox`),
  default `false`.
- On 409 show "主代碼已存在。".
- No `forkJoin` lookups needed (no FKs); component can load directly.

### Sidebar placement

Add 發布狀態 PublishStatus under the existing **系統管理 Admin** nav group in `app.ts`,
routed to `/publish-statuses` (next to 角色 AppRole).

---

## Files to Create / Modify

### Backend (`CMS.API`)

| File | Action |
|------|--------|
| `Models/PublishStatus.cs` | create |
| `Models/PublishStatusRequest.cs` | create |
| `Models/PublishStatusQuery.cs` | create |
| `Models/PublishStatusLookup.cs` | create |
| `Repositories/IPublishStatusRepository.cs` | create |
| `Repositories/PublishStatusRepository.cs` | create |
| `Controllers/PublishStatusesController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify (add `GetPublishStatusesAsync`) |
| `Repositories/LookupRepository.cs` | modify |
| `Controllers/LookupsController.cs` | modify (add `publish-statuses` route) |
| `Program.cs` | modify (register `IPublishStatusRepository`) |

### Frontend (`CMS.NG`)

| File | Action |
|------|--------|
| `core/models/publish-status.model.ts` | create |
| `core/services/publish-status.service.ts` | create |
| `features/publish-statuses/publish-status-list/*` (ts/html/scss) | create |
| `features/publish-statuses/publish-status-detail/*` (ts/html/scss) | create |
| `features/publish-statuses/publish-status-form/*` (ts/html/scss) | create |
| `app.routes.ts` | modify (4 routes) |
| `app.ts` | modify (sidebar entry) |

### Tests

| File | Action |
|------|--------|
| `CMS.API.Tests/PublishStatusesControllerTests.cs` | create |
| `CMS.API.Tests/Fakes/FakePublishStatusRepository.cs` | create |
| `CMS.API.Tests/AppRoleApiFactory.cs` | modify (swap in fake `IPublishStatusRepository`) |
| `CMS.API.Tests/Fakes/FakeLookupRepository.cs` | modify (implement `GetPublishStatusesAsync`) |
| `core/services/publish-status.service.spec.ts` | create |
| `features/.../publish-status-list.spec.ts` | create |
| `features/.../publish-status-detail.spec.ts` | create |
| `features/.../publish-status-form.spec.ts` | create |
