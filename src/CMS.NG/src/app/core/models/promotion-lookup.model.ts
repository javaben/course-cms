/** Result of resolving a Promotion2.PromoCode on the FeaturedPromoItem edit form. */
export interface PromotionLookup {
  pkid: number;
  promoCode: string;
  topic: string;
  description: string;
}
