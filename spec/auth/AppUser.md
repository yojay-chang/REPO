# Build Spec for AppUser
- database schema: `.\database\auth.sql`

## Summary

`AppUser` is a system login account. Its business PK is the string `UserId`; `pkid` is an
IDENTITY display code (主代碼). Each user has a `UserName`, an `IsActive` flag, and an internal
`PasswordHash` (SHA-256, **backend-only** — never sent to or accepted from the frontend). A user
is assigned many roles through the `AppUserRole` junction (N-N with `AppRole`). Mirror of the
AppRole reference feature (string PK + identity display code + N-N), with a bool column
(`IsActive`, like PublishStatus) and a server-managed password.

| Item | Detail |
|------|--------|
| Primary Key | `UserId` nvarchar(200) (business PK); `pkid` int IDENTITY is a display code |
| Foreign Keys | None on AppUser itself |
| Required Fields | `UserId`, `UserName`, `IsActive` (`PasswordHash` is server-managed, not user input) |
| N-N Relationships | `AppUserRole` — AppUser ↔ AppRole (`UserId`, `RoleId`) |
| Primary-Foreign Links | N/A — the only referencing table, `AppUserRole`, is the N-N junction |
| Query Filters | keyword (UserId, UserName); IsActive (tri-state); RoleId (assigned-role EXISTS filter) |
| Default Sort | `UserId ASC` |

---

## Localization

### Chinese Table Name

- AppUser: 使用者
- Description: 系統登入帳號

### Chinese Column Names

- pkid: 主代碼
- UserId: 使用者代碼
- UserName: 使用者名稱
- IsActive: 啟用
- PasswordHash: 密碼雜湊（後端專用，不對前端輸出）
- PasswordUpdatedTime: 密碼更新時間

---

## Required Fields

Required (NOT NULL, user-supplied):
- `UserId` NOT NULL (business PK; immutable in edit)
- `UserName` NOT NULL
- `IsActive` NOT NULL (bit; DB default 1)

Server-managed (NOT in Request, not user input):
- `PasswordHash` NOT NULL — set on CREATE from the default password; changed only via reset-password.

Optional (nullable, read-only in UI):
- `PasswordUpdatedTime` (datetime) — stamped on CREATE and on password reset.

---

## Foreign Keys

`AppUser` has no outbound foreign key columns.

**N/A**

---

## Foreign-Primary Links

`AppUser` has no outbound foreign key columns.

**N/A**

---

## Primary-Foreign Links

The only table referencing `AppUser` is `AppUserRole` (`UserId`), which is the N-N junction to
`AppRole` and is managed inline via the N-N Relationships section below. No separate child-list
navigation links are needed.

**N/A**

---

## N-N Relationships

### AppUserRole — AppUser ↔ AppRole

Junction table: `AppUserRole` (`UserId`, `RoleId`) — composite PK, FKs to `AppUser.UserId` and
`AppRole.RoleId`. This is the mirror image of AppRole's user assignment.

- **List view**: show a role count (`RoleCount`) via `(SELECT COUNT(*) FROM AppUserRole ur WHERE
  ur.UserId = u.UserId)`.
- **Detail view**: show the assigned roles as labels (`RoleName (RoleId)`), resolved against the
  app-roles lookup.
- **Form (edit + new)**: `p-multiselect` of roles (option label `RoleName (RoleId)`, value
  `roleId`), `[maxSelectedLabels]="9999"`, `appendTo="body"`, `[filter]="true"`.
- Request field: `RoleIds` (`List<string>`), populated on GET-by-id.
- **Sync pattern** (create & update), inside the same transaction:
  1. `DELETE FROM AppUserRole WHERE UserId = @UserId`
  2. Bulk `INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)` per distinct role.
- **Delete**: remove `AppUserRole` rows first (FK constraint), then the `AppUser` row.

---

## Query Filters

- **keyword**: string — LIKE on `UserId`, `UserName`.
- **IsActive**: bool? — exact match, tri-state (null = 全部, true = 是, false = 否).
- **RoleId**: string? — users assigned a given role:
  `EXISTS (SELECT 1 FROM AppUserRole ur WHERE ur.UserId = u.UserId AND ur.RoleId = @RoleId)`.
  Dropdown from `GET /api/lookups/app-roles`, option label `RoleName (RoleId)`, order by `RoleName`.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/app-roles` | **New** | Slim AppRole list (`RoleId`, `RoleName`) for the roles multiselect + role filter |

(`GET /api/lookups/app-users` already exists — served for AppRole's user multiselect; unrelated here.)

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/app-users` | List all (with `RoleCount`) |
| `POST` | `/api/app-users/query` | Filtered query (body: `AppUserQuery`) |
| `GET` | `/api/app-users/{id}` | Get by UserId (includes `RoleIds`) |
| `POST` | `/api/app-users` | Create (hashes default password into `PasswordHash`) |
| `PUT` | `/api/app-users` | Update (UserId in body; **never touches `PasswordHash`**) |
| `DELETE` | `/api/app-users/{id}` | Delete (removes `AppUserRole` rows first) |
| `POST` | `/api/app-users/{id}/reset-password` | Reset `PasswordHash` to the default password; stamp `PasswordUpdatedTime` |

- `{id}` route param is the string `UserId` — **no `:int` constraint**; service `encodeURIComponent`s it.
- **409 Conflict** on POST when `UserId` already exists; **404** on reset/update/get of a missing user.

### PasswordHash rules (backend only)

- `PasswordHash` is excluded from `AppUserRequest` and every Angular model — never serialized to
  or read from the frontend.
- **CREATE**: read `SysConfig.configValue` where `configKey = 'appConfig'` (a JSON object), parse
  it, extract the `defaultPassword` property, SHA-256-hash that string, store as `PasswordHash`,
  and set `PasswordUpdatedTime = GETDATE()`.
- **UPDATE**: leave `PasswordHash` and `PasswordUpdatedTime` untouched.
- **reset-password**: recompute the SHA-256 of the current `defaultPassword` and write it, stamping
  `PasswordUpdatedTime = GETDATE()`.

---

## Backend Notes

### Models

```csharp
public class AppUser
{
    public int Pkid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? PasswordUpdatedTime { get; set; }   // read-only, server-managed
    // NOTE: PasswordHash is intentionally NOT on the response model.

    public int RoleCount { get; set; }                    // AppUserRole subquery count (list)
    public List<string> RoleIds { get; set; } = [];       // populated on GET by id
}

public class AppUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> RoleIds { get; set; } = [];
    // NOTE: no PasswordHash — server-managed only.
}

public class AppUserQuery
{
    public string? Keyword { get; set; }   // LIKE UserId, UserName
    public bool? IsActive { get; set; }    // tri-state
    public string? RoleId { get; set; }    // assigned-role EXISTS filter
}

public class AppRoleLookup            // new lookup model
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
```

### SQL — SELECT

```sql
SELECT u.pkid, u.UserId, u.UserName, u.IsActive, u.PasswordUpdatedTime,
       (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
FROM AppUser u
-- PasswordHash is never selected into the response model.
```

GET-by-id additionally loads role ids:
```sql
SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId;
```

### SQL — INSERT

```sql
INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
VALUES (@UserId, @UserName, @IsActive, @PasswordHash, @PasswordUpdatedTime);
-- @PasswordHash = SHA-256(defaultPassword from SysConfig 'appConfig'.defaultPassword)
-- @PasswordUpdatedTime = DateTime.Now
```
(`pkid` is IDENTITY — omitted; not needed as return since the string `UserId` is the PK.)

### SQL — UPDATE (never touches PasswordHash)

```sql
UPDATE AppUser
   SET UserName = @UserName,
       IsActive = @IsActive
 WHERE UserId = @UserId;
```

### SQL — reset-password

```sql
UPDATE AppUser
   SET PasswordHash = @PasswordHash,          -- SHA-256(defaultPassword)
       PasswordUpdatedTime = @PasswordUpdatedTime
 WHERE UserId = @UserId;
```

### N-N Sync Pattern

Inside the create/update transaction:
```sql
DELETE FROM AppUserRole WHERE UserId = @UserId;
-- then per distinct role:
INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId);
```

### Default-password helper

- `SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'` → JSON string.
- Parse with `System.Text.Json`; read the `defaultPassword` property.
- `PasswordHasher.Sha256(defaultPassword)` → lowercase hex string (new `Data/PasswordHasher.cs`).
- Repository injects `IDbConnectionFactory` only; the SysConfig read + hashing happen inside the
  repository (`CreateAsync` / `ResetPasswordAsync`).

### Special Column Notes

- `PasswordHash` nvarchar(800): server-only. Never on the response model, Request, or any TS model.
- `PasswordUpdatedTime` datetime NULL: `DateTime?`; Dapper maps `datetime` natively (no handler).
  Frontend appends `'Z'` when displaying (project datetime convention).
- No `date`/`time` columns → no DateOnly/TimeOnly handler needed here.

---

## Frontend Notes

### Angular models (`core/models/app-user.model.ts`)

```ts
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  passwordUpdatedTime: string | null;   // read-only
  roleCount: number;
  roleIds: string[];
}
export interface AppUserRequest {
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
  // no passwordHash
}
export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
  roleId?: string | null;
}
export interface AppRoleLookup { roleId: string; roleName: string; }
```

### Service (`core/services/app-user.service.ts`)

Standard six + `getAppRoles()` (`/lookups/app-roles`) + `resetPassword(userId)`
(`POST /app-users/{id}/reset-password`). `getById` / `delete` / `resetPassword`
`encodeURIComponent` the string `UserId`.

### Routes (`app.routes.ts`)

`app-users`, `app-users/new`, `app-users/:id/edit`, `app-users/:id` (new before `:id`).

### List component

- Columns: 主代碼, 使用者代碼, 使用者名稱, 啟用 (`pi pi-check`/`—`), 角色數 (`roleCount`), 密碼更新時間, 操作.
- Filter drawer: keyword input; 啟用 tri-state `p-select` (全部/是/否); 角色 `p-select` from
  app-roles lookup (`appendTo="body"`, `[filter]="true"`).
- Session keys: `app-user-list-filters`, `app-user-list-sort`, `app-user-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.userId}」？`

### Detail component

- 基本資料 card: 主代碼, 使用者代碼, 使用者名稱, 啟用 (`p-tag` 是/否), 密碼更新時間 (`+ 'Z'` display, — when null).
- 角色 card: chips of `RoleName (RoleId)`, resolved from the app-roles lookup (`forkJoin` getById +
  getAppRoles).
- Toolbar: 重設密碼 button → confirm dialog → `resetPassword(userId)` → success toast + reload.

### Form component

- Reactive Forms; `forkJoin({ user?, roles })`.
- Fields: `userId` (required, disabled in edit — read via `getRawValue()`), `userName` (required),
  `isActive` (`p-toggleswitch`, default true), `roleIds` (`p-multiselect`).
- **No password field** anywhere in the form (create uses the default password server-side).
- Edit-mode hint near the top: password is set from the system default on create and changed only
  via 重設密碼.

### Sidebar

Group **系統管理 Admin** already exists in `app.ts` with a disabled `使用者 AppUser` entry — wire
its `route: '/app-users'`.

---

## Files to Create / Modify

### Backend (CMS.API)
| File | Action |
|------|--------|
| `Models/AppUser.cs` | create |
| `Models/AppUserRequest.cs` | create |
| `Models/AppUserQuery.cs` | create |
| `Models/AppRoleLookup.cs` | create |
| `Data/PasswordHasher.cs` | create (SHA-256 helper) |
| `Repositories/IAppUserRepository.cs` | create |
| `Repositories/AppUserRepository.cs` | create |
| `Controllers/AppUsersController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify — add `GetAppRolesAsync` |
| `Repositories/LookupRepository.cs` | modify — add `GetAppRolesAsync` |
| `Controllers/LookupsController.cs` | modify — add `app-roles` endpoint |
| `Program.cs` | modify — register `IAppUserRepository` |

### Frontend (CMS.NG)
| File | Action |
|------|--------|
| `core/models/app-user.model.ts` | create |
| `core/services/app-user.service.ts` | create |
| `features/app-users/app-user-list/*` | create (ts/html/scss) |
| `features/app-users/app-user-detail/*` | create (ts/html/scss) |
| `features/app-users/app-user-form/*` | create (ts/html/scss) |
| `app.routes.ts` | modify — add app-users routes |
| `app.ts` | modify — set `/app-users` route on the AppUser nav item |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/Fakes/FakeAppUserRepository.cs` | create |
| `CMS.API.Tests/AppUsersControllerTests.cs` | create |
| `CMS.API.Tests/AppRoleApiFactory.cs` | modify — swap in FakeAppUserRepository |
| `CMS.API.Tests/Fakes/FakeLookupRepository.cs` | modify — add `GetAppRolesAsync` |
| `core/services/app-user.service.spec.ts` | create |
| `features/app-users/app-user-list/app-user-list.spec.ts` | create |
| `features/app-users/app-user-detail/app-user-detail.spec.ts` | create |
| `features/app-users/app-user-form/app-user-form.spec.ts` | create |
