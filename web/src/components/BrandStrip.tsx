import type { CSSProperties } from 'react';
import clsx from 'clsx';
import { BRANDS, type BrandFilter } from '../lib/brands';
import { useFilters } from '../state/filters';

const ALL_CHIP = { key: 'alle' as const, label: 'Alle', shortId: 'ALL', colorVar: '' };

export default function BrandStrip() {
  const { marke, setMarke } = useFilters();
  const items = [ALL_CHIP, ...BRANDS];

  return (
    <div
      className="brandstrip sticky z-[3] flex items-center gap-1.5 px-6 overflow-x-auto"
      style={{
        top: 'calc(var(--topbar-h) + var(--filterbar-h))',
        height: 'var(--brandstrip-h)',
        background: 'var(--bg)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <span
        className="text-[11px] font-medium uppercase mr-1"
        style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
      >
        Marke
      </span>
      {items.map((b) => {
        const active = marke === b.key;
        const isAll = b.key === 'alle';
        const style = isAll
          ? ({} as CSSProperties)
          : ({ '--bc': `var(${b.colorVar})` } as CSSProperties);
        return (
          <button
            key={b.key}
            type="button"
            aria-pressed={active}
            onClick={() => setMarke(b.key as BrandFilter)}
            className={clsx('brand-chip', active && 'active')}
            style={style}
          >
            {!isAll && <span className="dot" />}
            <span className="brand-chip-id">{b.shortId}</span>
            <span className="brand-chip-label">{b.label}</span>
          </button>
        );
      })}
    </div>
  );
}
