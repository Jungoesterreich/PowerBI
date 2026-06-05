/*
 * Frontend brand definitions.
 *
 * Keys MUST match the backend `JoeSync.Api.Brands.JoeBrands` keys
 * (idu, jin, joe, ms, sp, lux, to) — they are used as path segments
 * for /api/brands/{key}/... calls.
 *
 * `color` is the CSS variable name from tokens.css; everything in the
 * UI that needs to render brand-coloured elements reads it as
 * `var(--brand-xxx)` so that tokens.css stays the single source of truth.
 */

export type BrandKey = 'jin' | 'joe' | 'idu' | 'ms' | 'sp' | 'lux' | 'to';
export type BrandFilter = 'alle' | BrandKey;

export interface BrandDef {
  key: BrandKey;
  label: string;
  shortId: string;
  colorVar: string;
}

export const BRANDS: ReadonlyArray<BrandDef> = [
  { key: 'jin', label: 'Join-In',          shortId: 'JIN', colorVar: '--brand-jin' },
  { key: 'joe', label: 'JÖ',               shortId: 'JOE', colorVar: '--brand-joe' },
  { key: 'idu', label: 'Ich-und-Du',       shortId: 'IDU', colorVar: '--brand-idu' },
  { key: 'ms',  label: 'Mini-Spatzenpost', shortId: 'MS',  colorVar: '--brand-ms'  },
  { key: 'sp',  label: 'Spatzenpost',      shortId: 'SP',  colorVar: '--brand-sp'  },
  { key: 'lux', label: 'Lux',              shortId: 'LUX', colorVar: '--brand-lux' },
  { key: 'to',  label: 'Topic',            shortId: 'TO',  colorVar: '--brand-to'  },
];

const INDEX: Record<BrandKey, BrandDef> = Object.fromEntries(
  BRANDS.map((b) => [b.key, b]),
) as Record<BrandKey, BrandDef>;

export function findBrand(key: BrandFilter): BrandDef | null {
  return key === 'alle' ? null : INDEX[key] ?? null;
}

export function brandColor(key: BrandFilter): string {
  const b = findBrand(key);
  return b ? `var(${b.colorVar})` : 'var(--fg)';
}
