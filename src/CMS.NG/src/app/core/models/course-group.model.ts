/** CourseGroup 課程群組 — response model (mirrors CMS.API.Models.CourseGroup). */
export interface CourseGroup {
  pkid: number;
  description: string;
}

/** Write DTO for create/update. pkid is IDENTITY — used for update, ignored on create. */
export interface CourseGroupRequest {
  pkid: number;
  description: string;
}

/** Search DTO for list filtering. */
export interface CourseGroupQuery {
  keyword?: string | null;
}

/** Slim CourseGroup lookup row for FK dropdowns in referencing features (Course, PartnerCourseGroup). */
export interface CourseGroupLookup {
  pkid: number;
  description: string;
}
