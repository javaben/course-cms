// Small pure helpers for the FeaturedPromoItem board's Monday–Sunday week, working on ISO date
// strings (yyyy-MM-dd) with local-noon Date math so no timezone/DST shift can cross a day boundary.

/** Weekday captions Monday→Sunday (一..日) — index 0 is Monday. */
export const WEEKDAY_LABELS = ['一', '二', '三', '四', '五', '六', '日'] as const;

export function parseIso(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d, 12); // local noon
}

export function toIso(dt: Date): string {
  const y = dt.getFullYear();
  const m = `${dt.getMonth() + 1}`.padStart(2, '0');
  const d = `${dt.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addDays(iso: string, n: number): string {
  const dt = parseIso(iso);
  dt.setDate(dt.getDate() + n);
  return toIso(dt);
}

/** ISO Monday of the week containing `iso` (Mon=1 … Sun=0 in JS getDay()). */
export function mondayOf(iso: string): string {
  const dt = parseIso(iso);
  const day = dt.getDay();
  const delta = day === 0 ? -6 : 1 - day;
  return addDays(iso, delta);
}

/** Today's date as an ISO string (local). */
export function todayIso(): string {
  return toIso(new Date());
}

/** "M/D" with no leading zeros, e.g. "3/16" (matches the spec mockups). */
export function mmdd(iso: string): string {
  const dt = parseIso(iso);
  return `${dt.getMonth() + 1}/${dt.getDate()}`;
}

/** "M/D (weekday)" day heading, e.g. "3/16 (一)". `index` is 0-based from Monday. */
export function dayHeading(iso: string, index: number): string {
  return `${mmdd(iso)} (${WEEKDAY_LABELS[index]})`;
}
