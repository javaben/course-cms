/** A scheduled promo slot on the home page (上稿作業). `scheduleOn` is an ISO date string (yyyy-MM-dd). */
export interface FeaturedPromoItem {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  /** Promotion2.PromoCode, joined in for display. */
  promoCode: string;
  topic: string;
  description: string;
}

/** Write DTO. The form resolves the entered PromoCode to `promotionPkid` before submitting. */
export interface FeaturedPromoItemRequest {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
}

/** Board filter: the active TrainingCenter tab and the Monday of the selected week. */
export interface FeaturedPromoItemQuery {
  trainingCenterPkid?: number | null;
  weekStart?: string | null;
}

/** Slot-move payload: +1 moves the row down a slot, -1 moves it up. */
export interface MoveSlotRequest {
  direction: number;
}
