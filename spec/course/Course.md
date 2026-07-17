# Build Spec for Course
- database schema: `.\database\course.sql`

## Summary

`Course` (課程) is the central content entity of the course sub-system: a training course
offered by a partner. It carries identifying codes (`CourseId`, `ProdCourseId`), a title,
scheduling dates, pricing, long-form descriptive content, and display metadata. It is the
**first FK-consuming entity** in the CMS — it references `Partner`, `CourseGroup` (nullable),
and `PublishStatus` — and the first with **date** columns and **N-N** relations to
`Certification` and `JobCategory`. It is also an FK target: `CourseFAQ`, `CourseRelatedLink`,
and `HotCourse` reference `Course.pkid`.

This is the richest entity to date. It combines patterns from three references:
**Partner** (int/smallint IDENTITY PK, `SCOPE_IDENTITY()`), **AppRole** (n-n delete-then-reinsert
in a transaction), and **PublishStatus** (bool flag + tri-state filter).

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **int IDENTITY** (system-assigned → `SELECT CAST(SCOPE_IDENTITY() AS int)`) |
| Foreign Keys | `Partner_pkid` → `Partner.pkid`; `CourseGroup_pkid` → `CourseGroup.pkid` (**nullable**); `PublishStatus_pkid` → `PublishStatus.pkid` |
| Required Fields | `Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`, `DisplayOrder`, `Partner_pkid`, `PublishStatus_pkid`, `ScheduleOn`, `ScheduleOff`, `Hour`, `ListPrice`, `LearningCredit`, `CanRepeat` |
| N-N Relationships | `CourseInCertification` (Course ↔ Certification); `CourseJobCategories` (Course ↔ JobCategory) |
| Primary-Foreign Links | `CourseFAQ`, `CourseRelatedLink`, `HotCourse` reference `Course.pkid` (child features pending) |
| Query Filters | keyword (`Title`, `OfficialTitle`, `CourseId`, `ProdCourseId`, `FriendlyUrl`); `Partner_pkid`; `CourseGroup_pkid`; `PublishStatus_pkid`; `ScheduleOn` range; `ScheduleOff` range; `CanRepeat` |
| Default Sort | `DisplayOrder ASC` |

---

## Localization

### Chinese Table Name

- Course: 課程
- Description: 訓練課程主資料

### Chinese Column Names

Labels for the columns the user named on the list page take priority; the rest are inferred.

- pkid: 主代碼
- DisplayOrder: 顯示順序
- CourseId: 簡介代碼
- ProdCourseId: 科目代碼
- Title: 課程名稱
- OfficialTitle: 官方課程名稱
- FriendlyUrl: 友善網址
- Partner_pkid: 原廠 (FK → Partner.Name)
- CourseGroup_pkid: 課程群組 (FK → CourseGroup.Description)
- PublishStatus_pkid: 上架狀態 (FK → PublishStatus.Description)
- ScheduleOn: 上架日期
- ScheduleOff: 下架日期
- Hour: 時數
- ListPrice: 定價
- LearningCredit: 點數
- Material: 教材
- Objective: 課程目標
- Target: 適合對象
- Prerequisites: 先備知識
- Outline: 課程大綱
- TowardCertOrExam: 考照／認證說明
- Note: 備註
- OtherInfo: 其他資訊
- CanRepeat: 允許重聽
- (N-N) Certifications: 對應認證
- (N-N) JobCategories: 對應職類

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):

- `Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`, `DisplayOrder`,
  `Partner_pkid`, `PublishStatus_pkid`, `ScheduleOn`, `ScheduleOff`,
  `Hour`, `ListPrice`, `LearningCredit`, `CanRepeat`

Optional (nullable):

- `OfficialTitle`, `CourseGroup_pkid`, `Material`, `Objective`, `Target`,
  `Prerequisites`, `Outline`, `TowardCertOrExam`, `Note`, `OtherInfo`

Controller enforces the NOT-NULL **string** fields with `IsNullOrWhiteSpace` 400 guards
(following `PartnersController`): `Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`.
Numeric/date/bool NOT-NULL fields are enforced by model binding + DB.

---

## Foreign Keys

List and detail use the FK to display the related row's label; form uses it to populate
select options. `_pkid` FK columns are aliased to the C# property name in SELECT, and the
related label is JOINed in as a flat read-only column (no Dapper multi-map — see Backend Notes).

- **Partner_pkid** → `Partner.pkid` (NOT NULL)
  - Alias `c.Partner_pkid AS PartnerPkid`; JOIN `Partner.Name AS PartnerName`.
  - Dropdown: `GET /api/lookups/partners`; option label = `Name`; order by `DisplayOrder ASC`.

- **CourseGroup_pkid** → `CourseGroup.pkid` (**nullable** — allow "（無）" option)
  - Alias `c.CourseGroup_pkid AS CourseGroupPkid`; **LEFT** JOIN `CourseGroup.Description AS CourseGroupDescription`.
  - Dropdown: `GET /api/lookups/course-groups`; option label = `Description`; order by `pkid ASC`.

- **PublishStatus_pkid** → `PublishStatus.pkid` (NOT NULL, `tinyint` → `byte`)
  - Alias `c.PublishStatus_pkid AS PublishStatusPkid`; JOIN `PublishStatus.Description AS PublishStatusDescription`.
  - Dropdown: `GET /api/lookups/publish-statuses`; option label = `Description`; order by `pkid ASC`.

---

## Foreign-Primary Links

Navigation FROM Course TO each referenced parent's detail page. Shown in detail (and as a hint
in the form). Only render the CourseGroup link when `CourseGroupPkid` is not null.

- **Partner_pkid** → `/partners/{PartnerPkid}` (button 檢視原廠, icon `pi pi-building`)
- **CourseGroup_pkid** → `/course-groups/{CourseGroupPkid}` (button 檢視課程群組, icon `pi pi-sitemap`; only when not null)
- **PublishStatus_pkid** → `/publish-statuses/{PublishStatusPkid}` (button 檢視上架狀態, icon `pi pi-flag`)

---

## Primary-Foreign Links

Tables that reference `Course.pkid` as an FK target. Show navigation buttons in the Course
detail view, each linking to the child list pre-filtered by `coursePkid`. Per the Partner
precedent, wire these now even though the child features are pending.

- **CourseFAQ** (`CourseFAQ.Course_pkid`)
  - Column header: 對應課程問答 — button 查看課程問答 (icon `pi pi-question-circle`)
  - Link: `/course-faqs?coursePkid={pkid}` — query param `coursePkid`
- **CourseRelatedLink** (`CourseRelatedLink.Course_pkid`)
  - Column header: 對應相關連結 — button 查看相關連結 (icon `pi pi-link`)
  - Link: `/course-related-links?coursePkid={pkid}` — query param `coursePkid`
- **HotCourse** (`HotCourse.Course_pkid`)
  - Column header: 對應熱門課程 — button 查看熱門課程 (icon `pi pi-star`)
  - Link: `/hot-courses?coursePkid={pkid}` — query param `coursePkid`

> These child features do not exist yet; the buttons target their eventual routes. This does not
> block the Course build.

---

## N-N Relationships

Both junction tables are pure two-column junctions. Sync follows the AppRole pattern:
delete-then-reinsert inside the create/update transaction. The pkid lists are populated on
`GET /{id}` only (not on list/query).

### CourseInCertification — Course ↔ Certification

Junction: `CourseInCertification` (`Course_pkid` int, `Certification_pkid` int).

- Form (edit + new): `p-multiselect` of Certifications.
- Options: `GET /api/lookups/certifications` (**NEW** lookup) — `Certification` has
  `pkid` int, `Partner_pkid`, `Title` **nchar(100) NULL** → `RTRIM(Title)`; order by `pkid ASC`.
  Option label = `Title` (fall back to `#{pkid}` when null).
- Detail: display selected Certification titles as `p-tag`s (資料在 detail 由 lookup 對照顯示).
- Request field: `CertificationPkids` — `List<int>`.
- Sync: `DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid;` then bulk INSERT.

### CourseJobCategories — Course ↔ JobCategory

Junction: `CourseJobCategories` (`Course_pkid` int, `JobCategory_pkid` **smallint**).

- Form (edit + new): `p-multiselect` of JobCategories.
- Options: `GET /api/lookups/job-categories` (**NEW** lookup) — `JobCategory` has
  `pkid` smallint, `Description` nvarchar(70); order by `pkid ASC`. Option label = `Description`.
- Detail: display selected JobCategory descriptions as `p-tag`s.
- Request field: `JobCategoryPkids` — `List<short>`.
- Sync: `DELETE FROM CourseJobCategories WHERE Course_pkid = @Pkid;` then bulk INSERT.

---

## Query Filters

- **keyword** (string): LIKE on `Title`, `OfficialTitle`, `CourseId`, `ProdCourseId`, `FriendlyUrl`.
  (Long text columns — `Objective`, `Outline`, `TowardCertOrExam`, `Note`, `OtherInfo`,
  `Prerequisites`, `Material`, `Target` — excluded.)
- **PartnerPkid** (`short?`): exact match `Partner_pkid`; dropdown `GET /api/lookups/partners`.
- **CourseGroupPkid** (`short?`): exact match `CourseGroup_pkid`; dropdown `GET /api/lookups/course-groups`.
- **PublishStatusPkid** (`byte?`): exact match `PublishStatus_pkid`; dropdown `GET /api/lookups/publish-statuses`.
- **ScheduleOn** range: `ScheduleOnFrom` / `ScheduleOnTo` (`DateOnly?`) → `ScheduleOn >= @From AND ScheduleOn <= @To`.
- **ScheduleOff** range: `ScheduleOffFrom` / `ScheduleOffTo` (`DateOnly?`) → `ScheduleOff >= @From AND ScheduleOff <= @To`.
- **CanRepeat** (`bool?`): exact match; tri-state select (全部／是／否).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **Exists** | `PartnerLookup` (`pkid`, `Name`), order `DisplayOrder ASC` |
| `GET /api/lookups/course-groups` | **Exists** | `CourseGroupLookup` (`pkid`, `Description`), order `pkid ASC` |
| `GET /api/lookups/publish-statuses` | **Exists** | `PublishStatusLookup` (`pkid`, `Description`), order `pkid ASC` |
| `GET /api/lookups/certifications` | **New** | `CertificationLookup` (`pkid`, `Title` via `RTRIM`), order `pkid ASC` |
| `GET /api/lookups/job-categories` | **New** | `JobCategoryLookup` (`pkid`, `Description`), order `pkid ASC` |

The two new lookups add: models `CertificationLookup` / `JobCategoryLookup`, `ILookupRepository`
+ `LookupRepository` methods, `LookupsController` GET routes, and `FakeLookupRepository` seeds.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/courses` | List all (ORDER BY `DisplayOrder ASC`), FK labels JOINed |
| `POST` | `/api/courses/query` | Filtered query (body: `CourseQuery`) |
| `GET` | `/api/courses/{id}` | Get by pkid (`id` binds `int`, no `:int` constraint); includes N-N pkid lists |
| `POST` | `/api/courses` | Create (pkid assigned by IDENTITY; no `ExistsAsync`/409) |
| `PUT` | `/api/courses` | Update (pkid from body) |
| `DELETE` | `/api/courses/{id}` | Delete (also clears both junction tables) |

Plus new lookup routes: `GET /api/lookups/certifications`, `GET /api/lookups/job-categories`.

No auth exceptions.

---

## Backend Notes

### Models

```csharp
// Course.cs — response model. FK labels are read-only JOINed columns; N-N lists populated on GET by id.
public class Course
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }
    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    // JOINed display labels (read-only, not written):
    public string? PartnerName { get; set; }
    public string? CourseGroupDescription { get; set; }
    public string? PublishStatusDescription { get; set; }

    // N-N — populated on GET by id only:
    public List<int> CertificationPkids { get; set; } = [];
    public List<short> JobCategoryPkids { get; set; } = [];
}

// CourseRequest.cs — write DTO (Pkid used for UPDATE; ignored on INSERT).
public class CourseRequest
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }
    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }
    public List<int> CertificationPkids { get; set; } = [];
    public List<short> JobCategoryPkids { get; set; } = [];
}

// CourseQuery.cs — search DTO.
public class CourseQuery
{
    public string? Keyword { get; set; }
    public short? PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte? PublishStatusPkid { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public DateOnly? ScheduleOffFrom { get; set; }
    public DateOnly? ScheduleOffTo { get; set; }
    public bool? CanRepeat { get; set; }
}

// CertificationLookup.cs / JobCategoryLookup.cs — slim FK-dropdown rows.
public class CertificationLookup { public int Pkid { get; set; } public string? Title { get; set; } }
public class JobCategoryLookup  { public short Pkid { get; set; } public string Description { get; set; } = string.Empty; }
```

### SQL — SELECT

Shared column list JOINs the three parent labels (LEFT JOIN CourseGroup as it is nullable):

```sql
SELECT c.pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl,
       c.DisplayOrder, c.Partner_pkid AS PartnerPkid, c.CourseGroup_pkid AS CourseGroupPkid,
       c.PublishStatus_pkid AS PublishStatusPkid, c.ScheduleOn, c.ScheduleOff, c.Hour,
       c.ListPrice, c.LearningCredit, c.Material, c.Objective, c.Target, c.Prerequisites,
       c.Outline, c.TowardCertOrExam, c.Note, c.OtherInfo, c.CanRepeat,
       p.Name AS PartnerName, cg.Description AS CourseGroupDescription,
       ps.Description AS PublishStatusDescription
FROM Course c
     INNER JOIN Partner p        ON p.pkid  = c.Partner_pkid
     LEFT  JOIN CourseGroup cg   ON cg.pkid = c.CourseGroup_pkid
     INNER JOIN PublishStatus ps ON ps.pkid = c.PublishStatus_pkid
```

- `GetAllAsync`: `... ORDER BY c.DisplayOrder ASC`
- `QueryAsync`: append `WHERE` conjuncts per filter; `ORDER BY c.DisplayOrder ASC`
- `GetByIdAsync`: `... WHERE c.pkid = @Pkid`, then two follow-up queries on the same connection
  to fill `CertificationPkids` (`SELECT Certification_pkid ... ORDER BY Certification_pkid`) and
  `JobCategoryPkids` (`SELECT JobCategory_pkid ... ORDER BY JobCategory_pkid`).

Single-type Dapper `QueryAsync<Course>` maps every column (including the aliased labels) — **no
multi-map / splitOn needed**. This is the deliberate simplification over nested nav objects.

### SQL — INSERT

`pkid` is IDENTITY — omit it, return `SCOPE_IDENTITY()`; the N-N sync runs in the same transaction:

```sql
INSERT INTO Course (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
    Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, Hour,
    ListPrice, LearningCredit, Material, Objective, Target, Prerequisites, Outline,
    TowardCertOrExam, Note, OtherInfo, CanRepeat)
VALUES (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
    @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff, @Hour,
    @ListPrice, @LearningCredit, @Material, @Objective, @Target, @Prerequisites, @Outline,
    @TowardCertOrExam, @Note, @OtherInfo, @CanRepeat);
SELECT CAST(SCOPE_IDENTITY() AS int);
```

`CreateAsync` opens the connection, `BeginTransaction()`, INSERTs (via `ExecuteScalarAsync<int>`
on the tx), syncs both junctions, commits, returns the new `int`.

### SQL — UPDATE

Same writable columns as INSERT (pkid in WHERE, not SET). Runs in a transaction with both syncs:

```sql
UPDATE Course SET Title=@Title, OfficialTitle=@OfficialTitle, CourseId=@CourseId,
    ProdCourseId=@ProdCourseId, FriendlyUrl=@FriendlyUrl, DisplayOrder=@DisplayOrder,
    Partner_pkid=@PartnerPkid, CourseGroup_pkid=@CourseGroupPkid,
    PublishStatus_pkid=@PublishStatusPkid, ScheduleOn=@ScheduleOn, ScheduleOff=@ScheduleOff,
    Hour=@Hour, ListPrice=@ListPrice, LearningCredit=@LearningCredit, Material=@Material,
    Objective=@Objective, Target=@Target, Prerequisites=@Prerequisites, Outline=@Outline,
    TowardCertOrExam=@TowardCertOrExam, Note=@Note, OtherInfo=@OtherInfo, CanRepeat=@CanRepeat
WHERE pkid=@Pkid
```

If `affected == 0` → rollback, return false (not found). Else sync junctions, commit.

### N-N Sync Pattern

```sql
DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid;
INSERT INTO CourseInCertification (Course_pkid, Certification_pkid) VALUES (@Pkid, @CertificationPkid); -- per id
DELETE FROM CourseJobCategories  WHERE Course_pkid = @Pkid;
INSERT INTO CourseJobCategories  (Course_pkid, JobCategory_pkid)  VALUES (@Pkid, @JobCategoryPkid);   -- per id
```

Distinct the lists (mirror `AppRoleRepository.SyncUsersAsync`); skip INSERT when the list is empty.
DELETE also clears both junctions before deleting the Course row.

### Special Column Notes

- `pkid` is **int IDENTITY** (system-assigned) — same pattern as Partner but `int`, so
  `SCOPE_IDENTITY()` casts to `int` and `CreateAsync` returns `int`. **No** `ExistsAsync`/409.
- **`date` → `DateOnly`** (`ScheduleOn`, `ScheduleOff`): Dapper 2.1.66 + Microsoft.Data.SqlClient
  6.0.1 handle `DateOnly` natively (both as parameters and results), and System.Text.Json (.NET 9)
  serializes `DateOnly` as `"yyyy-MM-dd"`. No custom Dapper type handler is required for this
  scaffold. (If a runtime mapping error surfaces, register a `DateOnlyTypeHandler` in `Program.cs`
  — not expected on these versions.)
- **`decimal`** (`ListPrice` `decimal(9,0)`, `LearningCredit` `decimal(9,1)`): plain `decimal`.
- `CanRepeat` `bit` → `bool`.
- `Certification.Title` is **nchar(100)** → `RTRIM()` in the certifications lookup SELECT.
- No computed columns.

### DI Registration

Add to `Program.cs`: `builder.Services.AddScoped<ICourseRepository, CourseRepository>();`

---

## Frontend Notes

### Angular Model (`core/models/course.model.ts`)

Mirror the C# model. `Course` includes `partnerName`, `courseGroupDescription`,
`publishStatusDescription` (labels), `certificationPkids`, `jobCategoryPkids`.
`CourseRequest` carries the FK pkids + both pkid arrays. `CourseQuery` carries the filters.
Add `CertificationLookup` / `JobCategoryLookup` to their own or the lookup model surface.
Dates cross the wire as `string` (`"yyyy-MM-dd"`).

### Service (`core/services/course.service.ts`)

Standard CRUD against `/api/courses`. **Numeric PK ⇒ no `encodeURIComponent`** in `getById`/`delete`.
Add `getCertifications()` and `getJobCategories()` to `core/services/lookup.service.ts`.

### Routes (`app.routes.ts`, `/new` before `/:id`)

| Path | Component |
|------|-----------|
| `/courses` | course-list |
| `/courses/new` | course-form (create) |
| `/courses/:id/edit` | course-form (edit) |
| `/courses/:id` | course-detail |

### List Component (`features/courses/course-list/`)

- Columns (user-specified): 主代碼 `pkid`, 顯示順序 `displayOrder`, 簡介代碼 `courseId`,
  科目代碼 `prodCourseId`, 課程名稱 `title`, 原廠 `partnerName`, 課程群組 `courseGroupDescription`,
  上架狀態 `publishStatusDescription`, 上架日期 `scheduleOn`, 下架日期 `scheduleOff`,
  時數 `hour`, 定價 `listPrice`, 點數 `learningCredit`, 允許重聽 `canRepeat`. Sortable/paginated `p-table`.
- `canRepeat` cell: `pi pi-check` / `—` (PublishStatus flag pattern). Dates shown as-is (`yyyy-MM-dd`).
- Filter drawer (`p-drawer`): keyword input; three FK `p-select`s (partners / course-groups /
  publish-statuses, `appendTo="body"`, mapped `{ pkid, label }[]`, `[filter]="true"`); two date-range
  pairs (`p-datepicker` from/to for ScheduleOn and ScheduleOff); `canRepeat` tri-state `p-select`.
- Load the three lookups via `forkJoin` on init; restore saved filters after they resolve.
- Session keys: `course-list-filters`, `course-list-sort`, `course-list-page`. Default sort `displayOrder` ASC.
- **Incoming query params** override saved filter state: `partnerPkid` (from Partner detail
  《查看課程》), also honour `courseGroupPkid` and `publishStatusPkid` if present.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.title}」？`

### Detail Component (`features/courses/course-detail/`)

- Show all scalar fields (long-text fields rendered as paragraphs; `—` when null).
- FK labels shown as the JOINed names; N-N shown as `p-tag` lists resolved via the certifications
  / job-categories lookups (load with `forkJoin` alongside the course).
- Foreign-Primary buttons: 檢視原廠 / 檢視課程群組 (only when set) / 檢視上架狀態.
- Primary-Foreign buttons: 查看課程問答 / 查看相關連結 / 查看熱門課程 (wired, pending features).
- Edit / Delete / Back actions.

### Form Component (`features/courses/course-form/`)

- Reactive Forms; `forkJoin` for all five lookups (partners, course-groups, publish-statuses,
  certifications, job-categories) + the course (edit) on init.
- Controls: `title` (input, required, max 200), `officialTitle` (input, max 300),
  `courseId` (input, required, max 50), `prodCourseId` (input, required, max 50),
  `friendlyUrl` (input, required, max 100), `displayOrder` (`p-inputnumber`, required),
  `partnerPkid` (`p-select`, required), `courseGroupPkid` (`p-select`, nullable — 顯示（無）option),
  `publishStatusPkid` (`p-select`, required), `scheduleOn`/`scheduleOff` (`p-datepicker`, required),
  `hour` (`p-inputnumber`, required), `listPrice` (`p-inputnumber` `[maxFractionDigits]=0`, required),
  `learningCredit` (`p-inputnumber` `[maxFractionDigits]=1`, required),
  `material`/`target` (input, max 500), `objective`/`prerequisites`/`note`/`otherInfo`
  (`textarea`), `outline`/`towardCertOrExam` (`textarea`, no max),
  `canRepeat` (`p-toggleswitch`), `certificationPkids` (`p-multiselect`),
  `jobCategoryPkids` (`p-multiselect`, `[maxSelectedLabels]="9999"`, `appendTo="body"`).
- **IDENTITY PK** → no pkid control on create (send `pkid: 0`); in edit show pkid read-only and
  read the writable form via `getRawValue()`.
- **Date handling**: convert ISO `string` ↔ `Date` on load/save. Serialize with **local** date
  components (`getFullYear()`/`getMonth()+1`/`getDate()`), never `toISOString()` (UTC+8 off-by-one).

### Sidebar Nav (`app.ts` / `app.html`)

Add to the existing **課程管理 Course** group, first item (above 合作廠商):

- 課程 Course → `/courses` (group already present; icon `pi pi-folder`)

### Bool / Date / Decimal display

- `canRepeat`: list `pi pi-check`/`—`; detail `p-tag` 是/否; form `p-toggleswitch`; filter tri-state `p-select`.
- Dates: `yyyy-MM-dd` text in list/detail; `p-datepicker` in form and filter ranges.
- `listPrice` integer, `learningCredit` one decimal place.

---

## Tests

### Backend (`CMS.API.Tests`)

- `CoursesControllerTests` (`IClassFixture<AppRoleApiFactory>`, HTTP-level like `PartnersControllerTests`):
  - `GetAll` → 200, ordered by DisplayOrder, FK labels populated (`PartnerName` etc.).
  - `Query` by keyword; by `PartnerPkid`; by `CanRepeat`; by `ScheduleOn` range.
  - `GetById` found (200, includes N-N lists) / missing (404).
  - `Create` valid → 201 + persists (pkid > 0); blank `Title`/`CourseId` → 400; N-N lists round-trip.
  - `Update` found → 200 (changes fields + N-N); missing → 404; blank `Title` → 400.
  - `Delete` found → 204 (then GET 404); missing → 404.
  - Lookups: `GET /api/lookups/certifications` and `/job-categories` return seeded rows.
- `FakeCourseRepository` (singleton, in-memory; assign `pkid = max+1`; store N-N lists; resolve
  FK labels from seeded parents) — follows `FakePartnerRepository`.
- `FakeLookupRepository`: add `GetCertificationsAsync` / `GetJobCategoriesAsync` seeds.
- `AppRoleApiFactory`: `RemoveAll<ICourseRepository>()` + register `FakeCourseRepository`.

### Frontend (`CMS.NG`)

- `course.service.spec.ts` — each method hits the right URL/verb; numeric PK ⇒ no `encodeURIComponent`.
- `course-list.spec.ts`, `course-detail.spec.ts`, `course-form.spec.ts` — mount with mocked
  `CourseService` + `LookupService`; assert render and required-field validation
  (`title`, `courseId`, `prodCourseId`, `friendlyUrl`, FKs, dates).

---

## Files to Create / Modify

### Backend
| File | Action |
|------|--------|
| `Models/Course.cs` | create |
| `Models/CourseRequest.cs` | create |
| `Models/CourseQuery.cs` | create |
| `Models/CertificationLookup.cs` | create |
| `Models/JobCategoryLookup.cs` | create |
| `Repositories/ICourseRepository.cs` | create |
| `Repositories/CourseRepository.cs` | create |
| `Controllers/CoursesController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify (add certifications, job-categories) |
| `Repositories/LookupRepository.cs` | modify |
| `Controllers/LookupsController.cs` | modify |
| `Program.cs` | modify (register `ICourseRepository`) |

### Frontend
| File | Action |
|------|--------|
| `core/models/course.model.ts` | create |
| `core/services/course.service.ts` | create |
| `core/services/lookup.service.ts` | modify (add certifications, job-categories) |
| `core/models/certification.model.ts` / `job-category.model.ts` | create (lookup interfaces) |
| `features/courses/course-list/*` | create |
| `features/courses/course-detail/*` | create |
| `features/courses/course-form/*` | create |
| `app.routes.ts` | modify (course routes) |
| `app.ts` / `app.html` | modify (課程 Course under 課程管理) |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/CoursesControllerTests.cs` | create |
| `CMS.API.Tests/Fakes/FakeCourseRepository.cs` | create |
| `CMS.API.Tests/Fakes/FakeLookupRepository.cs` | modify |
| `CMS.API.Tests/AppRoleApiFactory.cs` | modify |
| `CMS.NG/.../course.service.spec.ts` | create |
| `CMS.NG/.../course-list.spec.ts` / `course-detail.spec.ts` / `course-form.spec.ts` | create |
