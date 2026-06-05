import type { CSSProperties } from 'react';
import { findBrand, type BrandFilter, type BrandKey } from '../lib/brands';

interface Props {
  brand: BrandKey | BrandFilter | null | undefined;
  /** Show the full label after the ID badge. Default true. */
  showLabel?: boolean;
}

export default function BrandBadge({ brand, showLabel = true }: Props) {
  if (!brand || brand === 'alle') {
    return <span style={{ color: 'var(--fg-4)' }}>—</span>;
  }
  const def = findBrand(brand);
  if (!def) {
    return <span style={{ color: 'var(--fg-4)' }}>{brand}</span>;
  }
  return (
    <span
      className="inline-flex items-center gap-1.5 px-1.5 py-0.5 text-xs"
      style={{
        '--bc': `var(${def.colorVar})`,
        background: 'color-mix(in oklch, var(--bc) 10%, var(--surface))',
        border: '1px solid color-mix(in oklch, var(--bc) 20%, var(--border))',
        borderRadius: 999,
        color: 'var(--fg-2)',
      } as CSSProperties}
    >
      <span
        className="inline-block w-1.5 h-1.5 rounded-full"
        style={{ background: 'var(--bc)' }}
      />
      <span
        className="font-mono uppercase font-medium text-[10px]"
        style={{ color: 'var(--fg-2)' }}
      >
        {def.shortId}
      </span>
      {showLabel && <span>{def.label}</span>}
    </span>
  );
}
