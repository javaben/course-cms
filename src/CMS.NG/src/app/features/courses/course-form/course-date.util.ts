/**
 * Date helpers for the Course feature. Serialization uses **local** calendar components
 * (never `toISOString()`) so a UTC+8 date is not shifted a day backwards.
 */

/** Serialize a Date to a `yyyy-MM-dd` string using its local components. Null passes through. */
export function toIso(date: Date | null | undefined): string | null {
  if (!date) return null;
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Parse a `yyyy-MM-dd` string to a local Date (midnight). Null/empty passes through. */
export function fromIso(value: string | null | undefined): Date | null {
  if (!value) return null;
  const [y, m, d] = value.split('-').map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}

/** Return a new Date `n` years after `date` (same month/day). */
export function addYears(date: Date, n: number): Date {
  return new Date(date.getFullYear() + n, date.getMonth(), date.getDate());
}
