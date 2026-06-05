import { NavLink } from 'react-router-dom';
import clsx from 'clsx';
import type { ComponentType, ReactNode } from 'react';
import {
  IconHome,
  IconLibrary,
  IconFileText,
  IconDownload,
  IconHeadphones,
  IconRefresh,
  IconChevronDown,
  IconGlobe,
  IconPhone,
  IconUsers,
  IconBriefcase,
} from './Icons';

type IconComponent = ComponentType<{ size?: number }>;

interface ReportItem {
  to?: string;        // route — if missing, item is disabled
  label: string;
  Icon: IconComponent;
  hint?: string;      // small tag, e.g. "geplant", "bald"
}

interface ReportGroup {
  label: string;      // e.g. "jungoesterreich.at"
  items: ReportItem[];
}

interface Area {
  label: string;
  Icon: IconComponent;
  groups?: ReportGroup[];
  items?: ReportItem[];
}

const AREAS: Area[] = [
  {
    label: 'Websites',
    Icon: IconGlobe,
    groups: [
      {
        label: 'jungoesterreich.at',
        items: [
          { to: '/websites/jungoe/uebersicht',  label: 'Übersicht',     Icon: IconHome },
          { to: '/websites/jungoe/heftreihen',  label: 'Heftreihen',    Icon: IconLibrary },
          { to: '/websites/jungoe/pageviews',   label: 'Pageviews',     Icon: IconFileText },
          { to: '/websites/jungoe/downloads',   label: 'Downloads',     Icon: IconDownload },
          { to: '/websites/jungoe/media-audio', label: 'Audio & Video', Icon: IconHeadphones },
        ],
      },
      {
        label: 'Digi-Plattformen',
        items: [
          { label: 'joedigi.at',       Icon: IconFileText, hint: 'geplant' },
          { label: 'luxdigi.at',       Icon: IconFileText, hint: 'geplant' },
          { label: 'minispatzdigi.at', Icon: IconFileText, hint: 'geplant' },
          { label: 'spatzdigi.at',     Icon: IconFileText, hint: 'geplant' },
          { label: 'topicdigi.at',     Icon: IconFileText, hint: 'geplant' },
        ],
      },
    ],
  },
  {
    label: 'KSV',
    Icon: IconPhone,
    items: [
      { to: '/ksv/warteschlange', label: 'Warteschlange', Icon: IconPhone, hint: 'bald' },
      { label: 'Online-Konten / LERCHE', Icon: IconFileText, hint: 'geplant' },
    ],
  },
  {
    label: 'Sales',
    Icon: IconUsers,
    items: [
      { label: '360°-View Person',      Icon: IconUsers, hint: 'geplant' },
      { label: '360°-View Institution', Icon: IconUsers, hint: 'geplant' },
    ],
  },
  {
    label: 'Admin',
    Icon: IconBriefcase,
    items: [
      { to: '/admin/sync', label: 'Sync', Icon: IconRefresh },
    ],
  },
];

export default function Sidebar() {
  return (
    <aside
      className="sidebar flex flex-col"
      style={{
        width: 'var(--sidebar-w)',
        background: 'var(--surface)',
        borderRight: '1px solid var(--border)',
      }}
    >
      <header
        className="px-4 py-4 flex items-center gap-2 border-b"
        style={{ borderColor: 'var(--border)' }}
      >
        <span
          className="flex items-center justify-center"
          style={{
            width: 28,
            height: 28,
            background: 'var(--fg)',
            color: 'var(--surface)',
            borderRadius: 4,
            fontFamily: '"Instrument Serif", serif',
            fontStyle: 'italic',
            fontSize: 18,
            lineHeight: 1,
          }}
        >
          jö
        </span>
        <div className="flex flex-col leading-tight">
          <span className="text-sm font-semibold" style={{ color: 'var(--fg)' }}>
            JoeSync
          </span>
          <span
            className="text-[11px]"
            style={{ color: 'var(--fg-3)', fontFamily: '"Geist Mono", monospace' }}
          >
            Dashboard
          </span>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto pb-4">
        {AREAS.map((area) => (
          <AreaBlock key={area.label} area={area} />
        ))}
      </div>

      <footer
        className="px-3 py-3 flex items-center gap-2 border-t"
        style={{ borderColor: 'var(--border)' }}
      >
        <span
          className="flex items-center justify-center text-xs font-medium"
          style={{
            width: 28,
            height: 28,
            background: 'var(--surface-2)',
            color: 'var(--fg-2)',
            borderRadius: '50%',
            border: '1px solid var(--border)',
          }}
        >
          DN
        </span>
        <span className="text-sm flex-1" style={{ color: 'var(--fg-2)' }}>
          Dominik
        </span>
        <IconChevronDown size={14} />
      </footer>
    </aside>
  );
}

function AreaBlock({ area }: { area: Area }) {
  const Icon = area.Icon;
  return (
    <section className="mt-1">
      <div
        className="px-4 pt-4 pb-2 flex items-center gap-2 text-[11px] font-medium uppercase"
        style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
      >
        <Icon size={12} />
        <span>{area.label}</span>
      </div>

      {area.groups?.map((g) => (
        <Group key={g.label} group={g} />
      ))}

      {area.items && (
        <nav className="px-2">
          {area.items.map((item) => (
            <NavItem key={item.label} item={item} />
          ))}
        </nav>
      )}
    </section>
  );
}

function Group({ group }: { group: ReportGroup }) {
  return (
    <div className="mt-1">
      <div
        className="px-4 pt-2 pb-1 text-[10px]"
        style={{
          color: 'var(--fg-4)',
          fontFamily: '"Geist Mono", monospace',
        }}
      >
        {group.label}
      </div>
      <nav className="px-2">
        {group.items.map((item) => (
          <NavItem key={item.label} item={item} />
        ))}
      </nav>
    </div>
  );
}

function NavItem({ item }: { item: ReportItem }) {
  const Icon = item.Icon;
  const inner = (
    <>
      <Icon size={16} />
      <span className="flex-1">{item.label}</span>
      {item.hint && <HintTag>{item.hint}</HintTag>}
    </>
  );

  if (!item.to) {
    return (
      <button type="button" className="nav-item w-full text-left" disabled>
        {inner}
      </button>
    );
  }
  return (
    <NavLink
      to={item.to}
      className={({ isActive }) => clsx('nav-item', isActive && 'is-active')}
    >
      {inner}
    </NavLink>
  );
}

function HintTag({ children }: { children: ReactNode }) {
  return (
    <span
      className="text-[9px] uppercase font-medium px-1.5 py-0.5"
      style={{
        background: 'var(--surface-2)',
        border: '1px solid var(--border)',
        borderRadius: 999,
        color: 'var(--fg-3)',
        letterSpacing: '0.06em',
      }}
    >
      {children}
    </span>
  );
}
