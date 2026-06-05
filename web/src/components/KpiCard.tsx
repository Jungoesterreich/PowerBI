import type { ReactNode } from 'react';
import clsx from 'clsx';
import Sparkline from './Sparkline';
import type { Delta } from '../lib/metrics';
import { formatPercent } from '../lib/format';

export type KpiVariant = 'asymmetric' | 'value-first' | 'label-first';

interface Props {
  label: string;
  value: ReactNode;
  /** Optional helper text under the value. */
  hint?: ReactNode;
  /** Sparkline data points (newest last). */
  spark?: ReadonlyArray<number>;
  /** Delta vs. previous period — renders a coloured pill. */
  delta?: Delta;
  /** Optional override of card layout; defaults to the global data-kpi attribute. */
  variant?: KpiVariant;
  /** Optional brand colour so the sparkline matches a brand. */
  brandColor?: string;
}

export default function KpiCard({
  label,
  value,
  hint,
  spark,
  delta,
  variant,
  brandColor,
}: Props) {
  return (
    <div
      className="kpi-card"
      data-kpi={variant}
      style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--r-md)',
        padding: 'var(--pad-card)',
        boxShadow: 'var(--shadow-sm)',
      }}
    >
      <div
        className="text-[11px] font-medium uppercase"
        style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
      >
        {label}
      </div>

      <div className="mt-2 flex items-end justify-between gap-3">
        <div
          className="num"
          style={{
            fontSize: 30,
            lineHeight: 1.1,
            color: 'var(--fg)',
            fontWeight: 500,
            letterSpacing: '-0.01em',
          }}
        >
          {value}
        </div>
        {spark && spark.length > 1 && (
          <Sparkline data={spark} color={brandColor ?? 'var(--fg-3)'} />
        )}
      </div>

      {(delta || hint) && (
        <div className="mt-3 flex items-center gap-2">
          {delta && <DeltaPill delta={delta} />}
          {hint && (
            <span className="text-xs" style={{ color: 'var(--fg-3)' }}>
              {hint}
            </span>
          )}
        </div>
      )}
    </div>
  );
}

function DeltaPill({ delta }: { delta: Delta }) {
  const color =
    delta.direction === 'pos'
      ? 'var(--pos)'
      : delta.direction === 'neg'
        ? 'var(--neg)'
        : 'var(--fg-3)';
  const sign = delta.direction === 'pos' ? '+' : delta.direction === 'neg' ? '−' : '±';
  const pctLabel = delta.pct === null
    ? '—'
    : formatPercent(Math.abs(delta.pct) * 100, 1).replace(' %', '%');

  return (
    <span
      className={clsx('inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5')}
      style={{
        color,
        background: `color-mix(in oklch, ${color} 12%, var(--surface))`,
        border: `1px solid color-mix(in oklch, ${color} 25%, var(--border))`,
        borderRadius: 999,
        fontFamily: '"Geist Mono", monospace',
        fontWeight: 500,
      }}
    >
      {sign}
      {pctLabel}
    </span>
  );
}
