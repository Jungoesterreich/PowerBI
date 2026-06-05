import type { ReactNode, CSSProperties } from 'react';
import { useFilters } from '../state/filters';
import { findBrand } from '../lib/brands';

interface Props {
  title: string;
  actions?: ReactNode;
  /** Override the sub-line (default: zeitraum + schuljahr). */
  sub?: ReactNode;
}

function daysBetween(fromIso: string, toIso: string): number {
  const from = new Date(fromIso);
  const to = new Date(toIso);
  return Math.max(0, Math.round((to.getTime() - from.getTime()) / 86_400_000)) + 1;
}

function fmtDateShort(iso: string): string {
  if (!iso) {
    return '–';
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return d.toLocaleDateString('de-AT', { day: '2-digit', month: 'short', year: 'numeric' });
}

export default function PageHeader({ title, actions, sub }: Props) {
  const { from, to, schuljahr, marke } = useFilters();
  const brand = findBrand(marke);

  return (
    <header
      className="flex flex-wrap items-end justify-between gap-4 pb-4 mb-6"
      style={{ borderBottom: '1px solid var(--border)' }}
    >
      <div className="min-w-0">
        <div className="flex items-baseline gap-3 flex-wrap">
          <h1
            style={{
              fontFamily: '"Instrument Serif", serif',
              fontSize: 38,
              lineHeight: 1.1,
              color: 'var(--fg)',
              fontWeight: 400,
            }}
          >
            {title}
          </h1>
          {brand && (
            <span
              className="inline-flex items-baseline gap-1.5 px-2.5 py-1 text-xs"
              style={{
                '--bc': `var(${brand.colorVar})`,
                background: 'color-mix(in oklch, var(--bc) 14%, var(--surface))',
                border: '1px solid color-mix(in oklch, var(--bc) 28%, var(--border))',
                borderRadius: 999,
                color: 'var(--fg)',
              } as CSSProperties}
            >
              <span
                className="font-mono uppercase font-medium text-[10px] px-1.5 py-0.5"
                style={{
                  background: 'color-mix(in oklch, var(--bc) 25%, var(--surface))',
                  borderRadius: 3,
                }}
              >
                {brand.shortId}
              </span>
              <span>{brand.label}</span>
            </span>
          )}
        </div>
        <div
          className="mt-1 text-sm flex items-center gap-2 flex-wrap"
          style={{ color: 'var(--fg-3)' }}
        >
          {sub ?? (
            <>
              <span style={{ fontFamily: '"Geist Mono", monospace' }}>
                {fmtDateShort(from)} → {fmtDateShort(to)}
              </span>
              <Sep />
              <span>{daysBetween(from, to)} Tage</span>
              {schuljahr && schuljahr !== 'alle' && (
                <>
                  <Sep />
                  <span>Schuljahr {schuljahr}</span>
                </>
              )}
            </>
          )}
        </div>
      </div>

      {actions && <div className="flex items-end gap-2">{actions}</div>}
    </header>
  );
}

function Sep() {
  return (
    <span aria-hidden="true" style={{ color: 'var(--fg-4)' }}>
      ·
    </span>
  );
}
