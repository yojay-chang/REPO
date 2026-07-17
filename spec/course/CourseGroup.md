# Build Spec for CourseGroup
- database schema: `.\database\course.sql`

## Summary

`CourseGroup` (課程群組) is a minimal reference entity — a named grouping used to categorise
courses. It carries only a system-assigned identity `pkid` and a single required `Description`.
It has **no** foreign keys and **no** N-N relationships. It is an **FK target**: `Course`
(`Course.CourseGroup_pkid`, nullable) and `PartnerCourseGroup` (`PartnerCourseGroup.CourseGroup_pkid`)
reference `CourseGroup.pkid`, so this feature also introduces the `GET /api/lookups/course-groups`
dropdown endpoint those features consume.

This is the simplest entity in the system so far — same shape as the **Partner** reference
(smallint IDENTITY PK, FK-target lookup, no FK / no N-N) but with a single data column.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (system-assigned → `SCOPE_IDENTITY()`) |
| Foreign Keys | None |
| Required Fields | `Description` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course`, `PartnerCourseGroup` reference `CourseGroup.pkid` |
| Query Filters | keyword (`Description`) |
| Default Sort | `pkid DESC` (no `DisplayOrder`/date column — inferred, newest first) |

---

## Localization

### Chinese Table Name

- CourseGroup: 課程群組
- Description: 課程群組主資料（用於課程分類的名稱群組）

### Chinese Column Names

- pkid: 主代碼
- Description: 群組名稱

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):

- `Description` NOT NULL (nvarchar(100))

Optional (nullable):

- None.

---

## Foreign Keys

`CourseGroup` has no foreign key columns.

**N/A**

---

## Foreign-Primary Links

`CourseGroup` has no foreign key columns.

**N/A**

---

## Primary-Foreign Links

The following tables reference `CourseGroup.pkid` as an FK target. Navigation buttons are shown
in the list and detail views. Each button links to the child entity's list page, pre-filtered by
`courseGroupPkid`.

- **Course** (`Course.CourseGroup_pkid`, nullable, `ON DELETE CASCADE`)
  - Column header: 對應課程
  - Button label: 查看課程 (icon: `pi pi-book`)
  - Link target: `/courses?courseGroupPkid={pkid}`
  - Query param the child list accepts: `courseGroupPkid`

- **PartnerCourseGroup** (`PartnerCourseGroup.CourseGroup_pkid`)
  - Column header: 對應廠商課程群組
  - Button label: 查看廠商課程群組 (icon: `pi pi-sitemap`)
  - Link target: `/partner-course-groups?courseGroupPkid={pkid}`
  - Query param: `courseGroupPkid`

> Note: these child features (courses, partner-course-groups) may not be built yet. The buttons
> target their eventual routes; wire them now so the links work as those features land. This does
> not block the CourseGroup build.

---

## N-N Relationships

`CourseGroup` participates in no pure junction tables. `PartnerCourseGroup` has its own IDENTITY
`pkid` plus `DisplayOrder`/`Description` columns, so it is a first-class entity (a Primary-Foreign
child), **not** an N-N junction.

**N/A**

---

## Query Filters

- **keyword**: string
  - LIKE on `Description`

No FK filters, bool filters, or date-range filters apply to this table.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/course-groups` | **New** | Slim CourseGroup list (`pkid`, `Description`), ordered `pkid ASC` — consumed by Course / PartnerCourseGroup FK dropdowns |

This feature **adds** the `course-groups` lookup (endpoint + `CourseGroupLookup` model +
repository method). No other lookups are required by CourseGroup itself.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/course-groups` | List all (ORDER BY `pkid DESC`) |
| `POST` | `/api/course-groups/query` | Filtered query (body: `CourseGroupQuery`) |
| `GET` | `/api/course-groups/{id}` | Get by pkid (`id` binds `short`, no `:int` constraint) |
| `POST` | `/api/course-groups` | Create (pkid assigned by IDENTITY) |
| `PUT` | `/api/course-groups` | Update (pkid from body) |
| `DELETE` | `/api/course-groups/{id}` | Delete |

Plus the lookup route:

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/lookups/course-groups` | Slim `CourseGroupLookup` list for dropdowns |

No auth exceptions.

---

## Backend Notes

### Models

```csharp
// CourseGroup.cs — response model
public class CourseGroup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}

// CourseGroupRequest.cs — write DTO (Pkid used for UPDATE; ignored on INSERT)
public class CourseGroupRequest
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}

// CourseGroupQuery.cs — search DTO
public class CourseGroupQuery
{
    public string? Keyword { get; set; }   // LIKE on Description
}

// CourseGroupLookup.cs — slim FK-dropdown row
public class CourseGroupLookup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

### SQL — SELECT

No aliases needed (no `_pkid` FK columns). Shared column list:

```sql
SELECT cg.pkid, cg.Description
FROM CourseGroup cg
```

- `GetAllAsync`: `... ORDER BY cg.pkid DESC`
- `QueryAsync`: append `WHERE cg.Description LIKE @Keyword` when keyword present; `ORDER BY cg.pkid DESC`
- `GetByIdAsync`: `... WHERE cg.pkid = @Pkid`

### SQL — INSERT

`pkid` is IDENTITY — omit it and return `SCOPE_IDENTITY()`:

```sql
INSERT INTO CourseGroup (Description)
VALUES (@Description);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

`CreateAsync` returns `short`. **No `ExistsAsync`/409 check** — DB assigns the pkid.

### SQL — UPDATE

```sql
UPDATE CourseGroup
   SET Description = @Description
 WHERE pkid = @Pkid
```

### N-N Sync Pattern

**N/A** — no junction tables.

### Special Column Notes

- `pkid` is **smallint IDENTITY** (system-assigned). Like Partner, CourseGroup does **not** check
  `ExistsAsync` on create — the DB assigns the pkid. Repository create returns `short` from
  `SCOPE_IDENTITY()`.
- `Description` is `nvarchar(100)` → no `RTRIM()` needed (only `nchar` requires it).
- No `date`/`time` columns → no Dapper type handlers needed.
- No nullable columns.

---

## Frontend Notes

### Angular Model

```ts
// core/models/course-group.model.ts
export interface CourseGroup {
  pkid: number;
  description: string;
}

export interface CourseGroupRequest {
  pkid: number;
  description: string;
}

export interface CourseGroupQuery {
  keyword?: string | null;
}

export interface CourseGroupLookup {
  pkid: number;
  description: string;
}
```

### Service

`core/services/course-group.service.ts` — standard CRUD against `/api/course-groups`.
Numeric PK ⇒ **no** `encodeURIComponent` in `getById`/`delete`.
Add `getCourseGroups()` to `core/services/lookup.service.ts` hitting `/api/lookups/course-groups`.

### Routes

Added to `app.routes.ts` (order: `/new` before `/:id`):

| Path | Component |
|------|-----------|
| `/course-groups` | course-group-list |
| `/course-groups/new` | course-group-form (create) |
| `/course-groups/:id` | course-group-detail |
| `/course-groups/:id/edit` | course-group-form (edit) |

### List Component

`features/course-groups/course-group-list/`

- Columns: 主代碼 (pkid), 群組名稱 (description). Sortable/paginated `p-table`.
- Primary-Foreign nav buttons per row (查看課程 / 查看廠商課程群組) → child lists.
- Filter drawer (`p-drawer`): single keyword input.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？`
- Session keys: `course-group-list-filters`, `course-group-list-sort`, `course-group-list-page`.
- Default sort: `pkid` DESC.

### Detail Component

`features/course-groups/course-group-detail/`

- Shows all fields (主代碼, 群組名稱).
- Primary-Foreign nav buttons (查看課程 / 查看廠商課程群組).
- Edit / Delete / Back actions.

### Form Component

`features/course-groups/course-group-form/`

- Reactive Forms. Controls:
  - 群組名稱 (description) — `input`, required, maxlength 100
- PK is IDENTITY (system-assigned) → **no** pkid control shown/entered on create.
  In edit mode, pkid is displayed read-only (not part of the writable form).
- No lookups to load ⇒ `forkJoin` not required (no FK dropdowns).

### Sidebar Nav

Add under the existing nav group **課程管理 Course** in `app.ts` / `app.html`
(created by the Partner feature):

- 課程群組 → `/course-groups` (icon suggestion: `pi pi-th-large`)

### Date Handling

**N/A** — no date/datetime columns.

---

## Tests

### Backend (`CMS.API.Tests`)

- `CourseGroupsControllerTests` — cover each endpoint:
  - `GetAll` returns 200 + list
  - `Query` with a keyword returns 200 + filtered list
  - `GetById` found → 200; not-found → 404
  - `Create` valid → 201 (CreatedAtAction); missing required field (blank `Description`) → 400
  - `Update` found → 200; not-found → 404; blank `Description` → 400
  - `Delete` found → 204; not-found → 404
  - Mock `ICourseGroupRepository` via a `FakeCourseGroupRepository` (following `FakePartnerRepository`).
- Extend `FakeLookupRepository` with `GetCourseGroupsAsync`.

### Frontend (`CMS.NG`)

- `course-group.service.spec.ts` — assert each method hits the right URL/verb (no `encodeURIComponent`,
  numeric PK).
- `course-group-list.spec.ts`, `course-group-detail.spec.ts`, `course-group-form.spec.ts` — mount
  with a mocked service; assert render + form required-field validation.

---

## Files to Create / Modify

### Backend
| File | Action |
|------|--------|
| `Models/CourseGroup.cs` | create |
| `Models/CourseGroupRequest.cs` | create |
| `Models/CourseGroupQuery.cs` | create |
| `Models/CourseGroupLookup.cs` | create |
| `Repositories/ICourseGroupRepository.cs` | create |
| `Repositories/CourseGroupRepository.cs` | create |
| `Controllers/CourseGroupsController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify (add `GetCourseGroupsAsync`) |
| `Repositories/LookupRepository.cs` | modify (add `GetCourseGroupsAsync`) |
| `Controllers/LookupsController.cs` | modify (add `GET course-groups`) |
| `Program.cs` | modify (register `ICourseGroupRepository`) |

### Frontend
| File | Action |
|------|--------|
| `core/models/course-group.model.ts` | create |
| `core/services/course-group.service.ts` | create |
| `core/services/lookup.service.ts` | modify (add `getCourseGroups`) |
| `features/course-groups/course-group-list/*` | create |
| `features/course-groups/course-group-detail/*` | create |
| `features/course-groups/course-group-form/*` | create |
| `app.routes.ts` | modify (add course-group routes) |
| `app.ts` / `app.html` | modify (sidebar group 課程管理 Course) |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/CourseGroupsControllerTests.cs` | create |
| `CMS.API.Tests/Fakes/FakeCourseGroupRepository.cs` | create |
| `CMS.API.Tests/Fakes/FakeLookupRepository.cs` | modify (add `GetCourseGroupsAsync`) |
| `CMS.NG/.../course-group.service.spec.ts` | create |
| `CMS.NG/.../course-group-list.spec.ts` | create |
| `CMS.NG/.../course-group-detail.spec.ts` | create |
| `CMS.NG/.../course-group-form.spec.ts` | create |
