import { useLocation } from 'react-router-dom';
import { IconSearch, IconHelp, IconBell, IconChevronRight, IconUpload } from './Icons';

const ROUTE_LABELS: Record<string, string> = {
  '/uebersicht': 'Übersicht',
  '/heftreihen': 'Heftreihen',
  '/pageviews': 'Pageviews',
  '/downloads': 'Downloads',
  '/media-audio': 'Audio & Video',
  '/sync': 'Sync',
};

export default function Topbar() {
  const { pathname } = useLocation();
  const pageLabel = ROUTE_LABELS[pathname] ?? '—';

  return (
    <header
      className="topbar flex items-center justify-between sticky top-0 z-[5] px-6"
      style={{
        height: 'var(--topbar-h)',
        background: 'var(--surface)',
        borderBottom: '1px solid var(--border)',
      }}
    >
      <nav className="flex items-center gap-1.5 text-sm" aria-label="Breadcrumb">
        <span style={{ color: 'var(--fg-3)' }}>JoeSync</span>
        <IconChevronRight size={12} style={{ color: 'var(--fg-4)' }} />
        <span style={{ color: 'var(--fg-3)' }}>Berichte</span>
        <IconChevronRight size={12} style={{ color: 'var(--fg-4)' }} />
        <span style={{ color: 'var(--fg)', fontWeight: 500 }}>{pageLabel}</span>
      </nav>

      <div className="flex items-center gap-1">
        <GhostButton aria-label="Suche"><IconSearch size={16} /></GhostButton>
        <GhostButton aria-label="Hilfe"><IconHelp size={16} /></GhostButton>
        <GhostButton aria-label="Benachrichtigungen"><IconBell size={16} /></GhostButton>
        <button
          type="button"
          className="ml-2 flex items-center gap-1.5 px-3 py-1.5 text-sm rounded font-medium"
          style={{
            background: 'var(--fg)',
            color: 'var(--surface)',
            borderRadius: 'var(--r)',
          }}
        >
          <IconUpload size={14} />
          Export
        </button>
      </div>
    </header>
  );
}

function GhostButton({ children, ...rest }: React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type="button"
      className="flex items-center justify-center w-8 h-8 rounded hover:bg-[var(--surface-2)]"
      style={{ color: 'var(--fg-2)', borderRadius: 'var(--r)' }}
      {...rest}
    >
      {children}
    </button>
  );
}
