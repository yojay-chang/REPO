/** AppUser 使用者 — response model (mirrors CMS.API.Models.AppUser). PasswordHash is never exposed. */
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  passwordUpdatedTime: string | null;
  roleCount: number;
  roleIds: string[];
}

/** Write DTO for create/update. No password — it is server-managed. */
export interface AppUserRequest {
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
}

/** Search DTO for list filtering. */
export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
  roleId?: string | null;
}

/** Slim AppRole lookup row for the user-roles multiselect and role filter. */
export interface AppRoleLookup {
  roleId: string;
  roleName: string;
}
