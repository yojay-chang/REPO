# Build Spec for Partner
- database schema: `.\database\course.sql`

## Summary

`Partner` (合作廠商) is a reference entity representing a training-course partner/vendor.
It carries a short `Name`, an `AppKey` code, two display-name variants (for the partner menu
and for the course detail page), a `DisplayOrder`, and an optional logo `ImageFilename`.
It has **no** foreign keys and **no** N-N relationships. It is an **FK target**: `Course`,
`Certification`, and `PartnerCourseGroup` all reference `Partner.pkid`, so this feature also
introduces the `GET /api/lookups/partners` dropdown endpoint those features consume.

This is a simple, self-contained entity — closest in shape to the **AppRole** reference
(identity PK, FK-target lookup) but with no N-N junction.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (system-assigned → `SCOPE_IDENTITY()`) |
| Foreign Keys | None |
| Required Fields | `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`, `DisplayOrder` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course`, `Certification`, `PartnerCourseGroup` reference `Partner.pkid` |
| Query Filters | keyword (`Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`) |
| Default Sort | `DisplayOrder ASC` |

---

## Localization

### Chinese Table Name

- Partner: 合作廠商
- Description: 課程合作廠商主資料

### Chinese Column Names

- pkid: 主代碼
- Name: 廠商名稱
- AppKey: 應用金鑰
- NameOnPartnerMenu: 廠商選單顯示名稱
- NameOnCourseDetailPage: 課程詳細頁顯示名稱
- DisplayOrder: 顯示順序
- ImageFilename: 圖片檔名

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):

- `Name` NOT NULL
- `AppKey` NOT NULL
- `NameOnPartnerMenu` NOT NULL
- `NameOnCourseDetailPage` NOT NULL
- `DisplayOrder` NOT NULL

Optional (nullable):

- `ImageFilename` NULL

---

## Foreign Keys

`Partner` has no foreign key columns.

**N/A**

---

## Foreign-Primary Links

`Partner` has no foreign key columns.

**N/A**

---

## Primary-Foreign Links

The following tables reference `Partner.pkid` as an FK target. Navigation buttons are shown in
the list and detail views. Each button links to the child entity's list page, pre-filtered by
`partnerPkid`.

- **Course** (`Course.Partner_pkid`)
  - Column header: 對應課程
  - Button label: 查看課程 (icon: `pi pi-book`)
  - Link target: `/courses?partnerPkid={pkid}`
  - Query param the child list accepts: `partnerPkid`

- **Certification** (`Certification.Partner_pkid`)
  - Column header: 對應認證
  - Button label: 查看認證 (icon: `pi pi-verified`)
  - Link target: `/certifications?partnerPkid={pkid}`
  - Query param: `partnerPkid`

- **PartnerCourseGroup** (`PartnerCourseGroup.Partner_pkid`)
  - Column header: 對應廠商課程群組
  - Button label: 查看課程群組 (icon: `pi pi-sitemap`)
  - Link target: `/partner-course-groups?partnerPkid={pkid}`
  - Query param: `partnerPkid`

> Note: these child features (courses, certifications, partner-course-groups) may not be built
> yet. The buttons target their eventual routes; wire them now so the links work as those
> features land. This does not block the Partner build.

---

## N-N Relationships

`Partner` participates in no pure junction tables. `PartnerCourseGroup` has its own IDENTITY
`pkid` plus `DisplayOrder`/`Description` columns, so it is a first-class entity (a
Primary-Foreign child), **not** an N-N junction.

**N/A**

---

## Query Filters

- **keyword**: string
  - LIKE on `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`
  - (`ImageFilename` excluded — not a useful search target)

No FK filters, bool filters, or date-range filters apply to this table.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **New** | Slim Partner list (`pkid`, `Name`), ordered `DisplayOrder ASC` — consumed by Course / Certification / PartnerCourseGroup FK dropdowns |

This feature **adds** the `partners` lookup (endpoint + `PartnerLookup` model + repository
method). No other lookups are required by Partner itself.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/partners` | List all (ORDER BY `DisplayOrder ASC`) |
| `POST` | `/api/partners/query` | Filtered query (body: `PartnerQuery`) |
| `GET` | `/api/partners/{id}` | Get by pkid (`id` binds `short`, no `:int` constraint) |
| `POST` | `/api/partners` | Create (pkid assigned by IDENTITY) |
| `PUT` | `/api/partners` | Update (pkid from body) |
| `DELETE` | `/api/partners/{id}` | Delete |

Plus the lookup route:

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/lookups/partners` | Slim `PartnerLookup` list for dropdowns |

No auth exceptions.

---

## Backend Notes

### Models

```csharp
// Partner.cs — response model
public class Partner
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}

// PartnerRequest.cs — write DTO (Pkid used for UPDATE; ignored on INSERT)
public class PartnerRequest
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}

// PartnerQuery.cs — search DTO
public class PartnerQuery
{
    public string? Keyword { get; set; }   // LIKE on Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage
}

// PartnerLookup.cs — slim FK-dropdown row
public class PartnerLookup
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### SQL — SELECT

No aliases needed (no `_pkid` FK columns). Shared column list:

```sql
SELECT p.pkid, p.Name, p.AppKey, p.NameOnPartnerMenu, p.NameOnCourseDetailPage,
       p.DisplayOrder, p.ImageFilename
FROM Partner p
```

- `GetAllAsync`: `... ORDER BY p.DisplayOrder ASC`
- `QueryAsync`: append `WHERE (p.Name LIKE @Keyword OR p.AppKey LIKE @Keyword OR p.NameOnPartnerMenu LIKE @Keyword OR p.NameOnCourseDetailPage LIKE @Keyword)` when keyword present; `ORDER BY p.DisplayOrder ASC`
- `GetByIdAsync`: `... WHERE p.pkid = @Pkid`

### SQL — INSERT

`pkid` is IDENTITY — omit it and return `SCOPE_IDENTITY()`:

```sql
INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

`CreateAsync` returns `short`.

### SQL — UPDATE

```sql
UPDATE Partner
   SET Name = @Name,
       AppKey = @AppKey,
       NameOnPartnerMenu = @NameOnPartnerMenu,
       NameOnCourseDetailPage = @NameOnCourseDetailPage,
       DisplayOrder = @DisplayOrder,
       ImageFilename = @ImageFilename
 WHERE pkid = @Pkid
```

### N-N Sync Pattern

**N/A** — no junction tables.

### Special Column Notes

- `pkid` is **smallint IDENTITY** (system-assigned). Unlike `PublishStatus` (user-assigned PK
  with an `ExistsAsync` 409 check), Partner does **not** check `ExistsAsync` on create — the DB
  assigns the pkid. Repository create returns `short` from `SCOPE_IDENTITY()`.
- No `nchar` columns → no `RTRIM()` needed.
- No `date`/`time` columns → no Dapper type handlers needed.
- `ImageFilename` is the only nullable column.

---

## Frontend Notes

### Angular Model

```ts
// core/models/partner.model.ts
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

export interface PartnerRequest {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

export interface PartnerQuery {
  keyword?: string | null;
}

export interface PartnerLookup {
  pkid: number;
  name: string;
}
```

### Service

`core/services/partner.service.ts` — standard CRUD against `/api/partners`.
Numeric PK ⇒ **no** `encodeURIComponent` in `getById`/`delete`.
Add `getPartners()` to `core/services/lookup.service.ts` hitting `/api/lookups/partners`.

### Routes

Added to `app.routes.ts` (order: `/new` before `/:id`):

| Path | Component |
|------|-----------|
| `/partners` | partner-list |
| `/partners/new` | partner-form (create) |
| `/partners/:id` | partner-detail |
| `/partners/:id/edit` | partner-form (edit) |

### List Component

`features/partners/partner-list/`

- Columns: 主代碼 (pkid), 廠商名稱 (name), 應用金鑰 (appKey), 廠商選單顯示名稱
  (nameOnPartnerMenu), 顯示順序 (displayOrder). Sortable/paginated `p-table`.
- Primary-Foreign nav buttons per row (查看課程 / 查看認證 / 查看課程群組) → child lists.
- Filter drawer (`p-drawer`): single keyword input.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.name}」？`
- Session keys: `partner-list-filters`, `partner-list-sort`, `partner-list-page`.
- Default sort: `displayOrder` ASC.

### Detail Component

`features/partners/partner-detail/`

- Shows all fields. `ImageFilename` rendered as text (—  when null).
- Primary-Foreign nav buttons in toolbar/section (查看課程 / 查看認證 / 查看課程群組).
- Edit / Delete / Back actions.

### Form Component

`features/partners/partner-form/`

- Reactive Forms. Controls:
  - 廠商名稱 (name) — `input`, required
  - 應用金鑰 (appKey) — `input`, required, maxlength 10
  - 廠商選單顯示名稱 (nameOnPartnerMenu) — `input`, required, maxlength 200
  - 課程詳細頁顯示名稱 (nameOnCourseDetailPage) — `input`, required, maxlength 50
  - 顯示順序 (displayOrder) — `p-inputnumber`, required
  - 圖片檔名 (imageFilename) — `input`, optional, maxlength 50
- PK is IDENTITY (system-assigned) → **no** pkid control shown/entered on create.
  In edit mode, pkid is displayed read-only (not part of the writable form).
- No lookups to load ⇒ `forkJoin` not required (no FK dropdowns).

### Sidebar Nav

Add under a new nav group **課程管理 Course** in `app.ts` / `app.html`:

- 合作廠商 → `/partners` (icon suggestion: `pi pi-building`)

### Date Handling

**N/A** — no date/datetime columns.

---

## Tests

### Backend (`CMS.API.Tests`)

- `PartnersControllerTests` — cover each endpoint:
  - `GetAll` returns 200 + list
  - `Query` with a keyword returns 200 + filtered list
  - `GetById` found → 200; not-found → 404
  - `Create` valid → 201 (CreatedAtAction); missing required field (e.g. blank `Name`) → 400
  - `Update` found → 200; not-found → 404; blank `Name` → 400
  - `Delete` found → 204; not-found → 404
  - Mock `IPartnerRepository` via a `FakePartnerRepository` (following `FakePublishStatusRepository`).
- Extend the lookups test surface / `FakeLookupRepository` with `GetPartnersAsync`.

### Frontend (`CMS.NG`)

- `partner.service.spec.ts` — assert each method hits the right URL/verb (no `encodeURIComponent`,
  numeric PK).
- `partner-list.spec.ts`, `partner-detail.spec.ts`, `partner-form.spec.ts` — mount with a mocked
  service; assert render + form required-field validation.

---

## Files to Create / Modify

### Backend
| File | Action |
|------|--------|
| `Models/Partner.cs` | create |
| `Models/PartnerRequest.cs` | create |
| `Models/PartnerQuery.cs` | create |
| `Models/PartnerLookup.cs` | create |
| `Repositories/IPartnerRepository.cs` | create |
| `Repositories/PartnerRepository.cs` | create |
| `Controllers/PartnersController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify (add `GetPartnersAsync`) |
| `Repositories/LookupRepository.cs` | modify (add `GetPartnersAsync`) |
| `Controllers/LookupsController.cs` | modify (add `GET partners`) |
| `Program.cs` | modify (register `IPartnerRepository`) |

### Frontend
| File | Action |
|------|--------|
| `core/models/partner.model.ts` | create |
| `core/services/partner.service.ts` | create |
| `core/services/lookup.service.ts` | modify (add `getPartners`) |
| `features/partners/partner-list/*` | create |
| `features/partners/partner-detail/*` | create |
| `features/partners/partner-form/*` | create |
| `app.routes.ts` | modify (add partner routes) |
| `app.ts` / `app.html` | modify (sidebar group 課程管理 Course) |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/PartnersControllerTests.cs` | create |
| `CMS.API.Tests/Fakes/FakePartnerRepository.cs` | create |
| `CMS.API.Tests/Fakes/FakeLookupRepository.cs` | modify (add `GetPartnersAsync`) |
| `CMS.NG/.../partner.service.spec.ts` | create |
| `CMS.NG/.../partner-list.spec.ts` | create |
| `CMS.NG/.../partner-detail.spec.ts` | create |
| `CMS.NG/.../partner-form.spec.ts` | create |
