import clsx from 'clsx';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import { useFilters, type Preset } from '../state/filters';

const PRESETS: { id: Preset; label: string }[] = [
  { id: '7T',  label: '7 Tage' },
  { id: '30T', label: '30 Tage' },
  { id: '90T', label: '90 Tage' },
  { id: 'SJ',  label: 'Schuljahr' },
];

export default function GlobalFilterBar() {
  const f = useFilters();
  const schuljahreQ = useQuery({
    queryKey: ['dim', 'schuljahre'],
    queryFn: () => api.schuljahre(),
  });

  return (
    <div
      className="globalfilterbar sticky z-[4] flex items-center gap-4 px-6"
      style={{
        top: 'var(--topbar-h)',
        height: 'var(--filterbar-h)',
        background: 'var(--surface)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <FieldLabel>Zeitraum</FieldLabel>
      <div className="flex items-center" style={{ gap: 1 }}>
        {PRESETS.map((p, idx) => (
          <button
            key={p.id}
            type="button"
            onClick={() => f.applyPreset(p.id)}
            aria-pressed={f.preset === p.id}
            className={clsx(
              'px-3 py-1.5 text-sm border',
              idx === 0 && 'rounded-l',
              idx === PRESETS.length - 1 && 'rounded-r',
            )}
            style={{
              background: f.preset === p.id ? 'var(--fg)' : 'var(--surface)',
              color: f.preset === p.id ? 'var(--surface)' : 'var(--fg-2)',
              borderColor: f.preset === p.id ? 'var(--fg)' : 'var(--border)',
              borderLeftWidth: idx === 0 ? 1 : 0,
              fontWeight: f.preset === p.id ? 500 : 400,
            }}
          >
            {p.label}
          </button>
        ))}
      </div>

      <div
        className="flex items-center text-xs"
        style={{
          border: '1px solid var(--border)',
          borderRadius: 'var(--r)',
          background: 'var(--surface)',
          height: 32,
        }}
      >
        <input
          type="date"
          value={f.from}
          onChange={(e) => f.setFrom(e.target.value)}
          className="bg-transparent px-2 py-1 outline-none"
          style={{ fontFamily: '"Geist Mono", monospace', color: 'var(--fg)' }}
        />
        <span style={{ color: 'var(--fg-4)' }}>—</span>
        <input
          type="date"
          value={f.to}
          onChange={(e) => f.setTo(e.target.value)}
          className="bg-transparent px-2 py-1 outline-none"
          style={{ fontFamily: '"Geist Mono", monospace', color: 'var(--fg)' }}
        />
      </div>

      <FieldLabel>Schuljahr</FieldLabel>
      <select
        value={f.schuljahr}
        onChange={(e) => f.applySchuljahr(e.target.value)}
        className="px-2 py-1.5 text-sm bg-transparent"
        style={{
          border: '1px solid var(--border)',
          borderRadius: 'var(--r)',
          color: 'var(--fg)',
          height: 32,
        }}
      >
        <option value="alle">alle</option>
        {schuljahreQ.data?.map((s) => (
          <option key={s} value={s}>{s}</option>
        ))}
      </select>

      <div className="ml-auto flex items-center gap-2">
        <Pill>JOEDB_KPI</Pill>
        <Pill>
          <span
            className="inline-block w-1.5 h-1.5 rounded-full mr-1.5"
            style={{ background: 'var(--pos)' }}
          />
          Synchron · vor 4 Min
        </Pill>
      </div>
    </div>
  );
}

function FieldLabel({ children }: { children: React.ReactNode }) {
  return (
    <span
      className="text-[11px] font-medium uppercase"
      style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
    >
      {children}
    </span>
  );
}

function Pill({ children }: { children: React.ReactNode }) {
  return (
    <span
      className="flex items-center px-2 py-0.5 text-xs"
      style={{
        background: 'var(--surface-2)',
        border: '1px solid var(--border)',
        borderRadius: 999,
        color: 'var(--fg-2)',
        fontFamily: '"Geist Mono", monospace',
      }}
    >
      {children}
    </span>
  );
}
