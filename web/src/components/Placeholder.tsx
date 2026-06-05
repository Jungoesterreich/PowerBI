import type { ReactNode } from 'react';

type Mode = 'empty' | 'loading' | 'error';

interface Props {
  mode?: Mode;
  title?: ReactNode;
  hint?: ReactNode;
  /** Inline SVG element shown in the icon tile. Optional. */
  icon?: ReactNode;
  /** When true, fills the height of the parent (default true). */
  fill?: boolean;
}

const DEFAULTS: Record<Mode, { title: string; hint: string }> = {
  empty:   { title: 'Keine Daten',  hint: 'Im gewählten Zeitraum sind keine Werte verfügbar.' },
  loading: { title: 'Lade Daten…',  hint: 'Einen Moment bitte.' },
  error:   { title: 'Fehler',       hint: 'Die Daten konnten nicht geladen werden.' },
};

export default function Placeholder({ mode = 'empty', title, hint, icon, fill = true }: Props) {
  const defs = DEFAULTS[mode];
  const isError = mode === 'error';
  return (
    <div
      className="flex flex-col items-center justify-center gap-2 text-center px-4 py-8"
      style={{
        height: fill ? '100%' : undefined,
        color: isError ? 'var(--neg)' : 'var(--fg-3)',
      }}
    >
      <div
        className="flex items-center justify-center"
        style={{
          width: 32,
          height: 32,
          borderRadius: 'var(--r)',
          background: isError
            ? 'color-mix(in oklch, var(--neg) 10%, var(--surface))'
            : 'var(--surface-2)',
        }}
      >
        {icon ?? <DefaultIcon mode={mode} />}
      </div>
      <div className="text-sm font-medium" style={{ color: isError ? 'var(--neg)' : 'var(--fg-2)' }}>
        {title ?? defs.title}
      </div>
      <div className="text-xs" style={{ color: 'var(--fg-3)' }}>
        {hint ?? defs.hint}
      </div>
    </div>
  );
}

function DefaultIcon({ mode }: { mode: Mode }) {
  if (mode === 'error') {
    return (
      <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.75}>
        <circle cx="12" cy="12" r="10" />
        <line x1="12" y1="8" x2="12" y2="12" />
        <line x1="12" y1="16" x2="12.01" y2="16" />
      </svg>
    );
  }
  if (mode === 'loading') {
    return (
      <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.75}>
        <path d="M21 12a9 9 0 1 1-6.219-8.56" />
      </svg>
    );
  }
  return (
    <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.75}>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  );
}
