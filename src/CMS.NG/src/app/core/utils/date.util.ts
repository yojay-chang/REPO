/** Date helpers for `date`-typed API fields (serialized as `yyyy-MM-dd`).
 *  Always uses LOCAL date components — never `toISOString()`, which shifts to UTC and
 *  produces an off-by-one day for UTC+8 users. */

/** Serialize a `Date` to a local `yyyy-MM-dd` string. */
export function toIsoDate(date: Date | null): string | null {
  if (!(date instanceof Date) || isNaN(date.getTime())) {
    return null;
  }
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Parse a `yyyy-MM-dd` string into a local `Date` (midnight local time). */
export function parseIsoDate(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  const [y, m, d] = value.split('-').map(Number);
  if (!y || !m || !d) {
    return null;
  }
  return new Date(y, m - 1, d);
}

/** A local, date-only copy of `date` advanced by `days` (may be negative). */
export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** The Monday (local, date-only) of the week that contains `date`. Weeks run Monday→Sunday. */
export function startOfWeekMonday(date: Date): Date {
  const day = date.getDay(); // 0=Sun, 1=Mon, … 6=Sat
  const backToMonday = day === 0 ? -6 : 1 - day;
  return addDays(date, backToMonday);
}
