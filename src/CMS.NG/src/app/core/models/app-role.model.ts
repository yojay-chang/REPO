/** AppRole 使用者角色 — response model (mirrors CMS.API.Models.AppRole). */
export interface AppRole {
  pkid: number;
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  userCount: number;
  userIds: string[];
}

/** Write DTO for create/update. */
export interface AppRoleRequest {
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  userIds: string[];
}

/** Search DTO for list filtering. */
export interface AppRoleQuery {
  keyword?: string | null;
  permissionLevelFrom?: number | null;
  permissionLevelTo?: number | null;
}

/** Slim AppUser lookup row for the role-users multiselect. */
export interface AppUserLookup {
  userId: string;
  userName: string;
}
