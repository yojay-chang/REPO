/** Partner 合作廠商 — response model (mirrors CMS.API.Models.Partner). */
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

/** Write DTO for create/update. pkid is IDENTITY — used for update, ignored on create. */
export interface PartnerRequest {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

/** Search DTO for list filtering. */
export interface PartnerQuery {
  keyword?: string | null;
}

/** Slim Partner lookup row for FK dropdowns in referencing features (Course, Certification, PartnerCourseGroup). */
export interface PartnerLookup {
  pkid: number;
  name: string;
}
