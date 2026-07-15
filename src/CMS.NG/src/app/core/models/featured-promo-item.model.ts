/** FeaturedPromoItem 上稿作業 — response model (mirrors CMS.API.Models.FeaturedPromoItem).
 *  One scheduled promotion slot for a training center on a given day. The FK values carry their
 *  JOINed display labels (`promoCode`, `trainingCenterName`).
 *  `scheduleOn` crosses the wire as an ISO `yyyy-MM-dd` string. */
export interface FeaturedPromoItem {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
  // JOINed display labels (read-only):
  promoCode: string | null;
  trainingCenterName: string | null;
}

/** Write DTO for create/update. pkid is IDENTITY — used for update, ignored on create. */
export interface FeaturedPromoItemRequest {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
}

/** Search DTO for the weekly scheduler: a TrainingCenter tab + a Monday–Sunday ScheduleOn range. */
export interface FeaturedPromoItemQuery {
  trainingCenterPkid?: number | null;
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
}
