/*
 * Comparison helpers for KPI cards.
 *
 * Vorperiode (previous period) = the same number of days immediately before
 * the current range. We compute the delta against the sum/avg of that window
 * so each KPI can show "vs. previous" without an extra backend call.
 */

import { isoDate } from './dates';

export interface Period {
  from: string; // ISO YYYY-MM-DD
  to: string;
}

export interface Delta {
  /** Absolute change (current - previous). */
  diff: number;
  /** Relative change as a unit fraction (e.g. 0.12 == +12 %). */
  pct: number | null;
  direction: 'pos' | 'neg' | 'flat';
}

const DAY_MS = 86_400_000;

function parseIso(iso: string): Date {
  return new Date(`${iso}T00:00:00Z`);
}

export function prevPeriod({ from, to }: Period): Period {
  const start = parseIso(from);
  const end = parseIso(to);
  const len = Math.round((end.getTime() - start.getTime()) / DAY_MS) + 1;
  const prevEnd = new Date(start.getTime() - DAY_MS);
  const prevStart = new Date(prevEnd.getTime() - (len - 1) * DAY_MS);
  return { from: isoDate(prevStart), to: isoDate(prevEnd) };
}

export function calcDelta(current: number, previous: number): Delta {
  const diff = current - previous;
  let pct: number | null = null;
  if (previous !== 0) {
    pct = diff / previous;
  }
  let direction: Delta['direction'] = 'flat';
  if (Math.abs(diff) > 1e-9) {
    direction = diff > 0 ? 'pos' : 'neg';
  }
  return { diff, pct, direction };
}

/** Pick a number field from each row, summing them. Skips null/undefined. */
export function sumField<T extends Record<string, unknown>>(
  rows: ReadonlyArray<T>,
  field: keyof T,
): number {
  let total = 0;
  for (const r of rows) {
    const v = r[field];
    if (typeof v === 'number' && Number.isFinite(v)) {
      total += v;
    }
  }
  return total;
}
