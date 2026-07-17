/** PublishStatus 發布狀態 — response model (mirrors CMS.API.Models.PublishStatus). */
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}

/** Write DTO for create/update. pkid is the user-assigned PK. */
export interface PublishStatusRequest {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}

/** Search DTO for list filtering. */
export interface PublishStatusQuery {
  keyword?: string | null;
  isDraft?: boolean | null;
  isPublished?: boolean | null;
  isDiscontinued?: boolean | null;
}

/** Slim PublishStatus lookup row for FK dropdowns in referencing features. */
export interface PublishStatusLookup {
  pkid: number;
  description: string;
}
