import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Bar,
  BarChart,
  Cell,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { api } from '../api/client';
import PageHeader from '../components/PageHeader';
import Placeholder from '../components/Placeholder';
import DataTable, { type Column } from '../components/DataTable';
import { useFilters } from '../state/filters';
import { findBrand } from '../lib/brands';
import { useDebounced } from '../lib/useDebounced';
import { formatNumber, truncate } from '../lib/format';
import {
  axisProps,
  cartesianGridProps,
  legendStyle,
  tooltipStyle,
  BRAND_HEX,
  SERIES_PALETTE,
} from '../lib/chart-theme';
import type { DownloadRow } from '../api/types';

const SITE_ID = 1;
const FILE_TYPES = ['', 'pdf', 'mp3', 'mp4', 'zip', 'doc', 'docx'];

export default function Downloads() {
  const { from, to, schuljahr, marke } = useFilters();
  const [ausgabe, setAusgabe] = useState('');
  const [fileType, setFileType] = useState('');
  const [searchPattern, setSearchPattern] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearch = useDebounced(searchTerm, 300);

  const isBrand = marke !== 'alle';
  const brand = findBrand(marke);

  const ausgabenQ = useQuery({
    queryKey: ['dim', 'ausgaben', schuljahr],
    queryFn: () => api.ausgaben(schuljahr === 'alle' ? undefined : schuljahr),
  });

  const begriffeQ = useQuery({
    queryKey: ['dim', 'search-terms', SITE_ID],
    queryFn: () => api.searchTerms(SITE_ID, 200),
  });

  const params = useMemo(
    () => ({
      siteId: SITE_ID,
      from,
      to,
      schuljahr: schuljahr === 'alle' ? undefined : schuljahr,
      ausgabe: ausgabe || undefined,
      fileType: fileType || undefined,
      searchPattern: searchPattern || undefined,
      searchTerm: debouncedSearch || undefined,
    }),
    [from, to, schuljahr, ausgabe, fileType, searchPattern, debouncedSearch],
  );

  const q = useQuery({
    queryKey: ['downloads', marke, params],
    queryFn: () =>
      isBrand
        ? api.brandDownloads(marke, { ...params, top: 200 })
        : api.downloads({ ...params, top: 200 }),
  });

  const brandTimelineQ = useQuery({
    queryKey: ['downloads', 'brand-timeline', marke, params],
    queryFn: () => api.brandTimeline(marke, params),
    enabled: isBrand,
  });

  const rows = q.data ?? [];

  const topBars = useMemo(
    () =>
      rows.slice(0, 12).map((r) => ({
        name: truncate(r.urlLastPathElement ?? r.url ?? '–', 32),
        Downloads: r.downloads,
      })),
    [rows],
  );

  const fileTypeMix = useMemo(() => {
    const acc = new Map<string, number>();
    for (const r of rows) {
      const key = (r.fileType ?? 'andere').toLowerCase();
      acc.set(key, (acc.get(key) ?? 0) + r.downloads);
    }
    return Array.from(acc.entries())
      .map(([name, value]) => ({ name: name.toUpperCase(), value }))
      .sort((a, b) => b.value - a.value);
  }, [rows]);

  const brandTimelineData = useMemo(
    () =>
      (brandTimelineQ.data ?? []).map((p) => ({
        date: p.date.slice(0, 10),
        Downloads: p.downloads,
      })),
    [brandTimelineQ.data],
  );

  const brandStroke = isBrand ? BRAND_HEX[marke] : SERIES_PALETTE[3];

  const columns: Column<DownloadRow>[] = [
    {
      key: 'url',
      label: 'Datei',
      render: (r) =>
        r.url ? (
          <a
            href={r.url}
            target="_blank"
            rel="noreferrer"
            className="hover:underline"
            style={{ color: 'var(--fg)' }}
            title={r.url}
          >
            {truncate(r.urlLastPathElement ?? r.url, 70)}
          </a>
        ) : (
          '–'
        ),
      sortValue: (r) => r.urlLastPathElement ?? r.url ?? '',
    },
    { key: 'fileType', label: 'Typ', render: (r) => (r.fileType ?? '–').toUpperCase() },
    { key: 'edition', label: 'Ausgabe', render: (r) => r.edition ?? '–' },
    { key: 'schuljahr', label: 'Schuljahr', render: (r) => r.schuljahr ?? '–' },
    {
      key: 'begriff',
      label: 'Begriff',
      render: (r) => r.searchPatternIdu ?? r.searchPatternTo ?? '–',
      sortValue: (r) => r.searchPatternIdu ?? r.searchPatternTo ?? '',
    },
    {
      key: 'downloads',
      label: 'Downloads',
      num: true,
      fg: true,
      render: (r) => formatNumber(r.downloads),
      sortValue: (r) => r.downloads,
    },
    {
      key: 'uniqueDownloads',
      label: 'Unique',
      num: true,
      render: (r) => formatNumber(r.uniqueDownloads),
      sortValue: (r) => r.uniqueDownloads,
    },
  ];

  return (
    <div>
      <PageHeader
        title="Downloads"
        sub={`Heruntergeladene Dateien ${isBrand ? `· ${brand?.label}` : 'über alle Marken'}`}
      />

      <div className="flex flex-wrap items-end gap-3 mt-1">
        <SubField label="Ausgabe">
          <select value={ausgabe} onChange={(e) => setAusgabe(e.target.value)} className="sub-select">
            <option value="">alle</option>
            {ausgabenQ.data?.map((a) => (
              <option key={a.ausgabenkuerzel} value={a.ausgabenkuerzel}>
                {a.ausgabenkuerzel} ({a.schuljahr})
              </option>
            ))}
          </select>
        </SubField>
        <SubField label="Dateityp">
          <select value={fileType} onChange={(e) => setFileType(e.target.value)} className="sub-select">
            {FILE_TYPES.map((t) => (
              <option key={t} value={t}>
                {t || 'alle'}
              </option>
            ))}
          </select>
        </SubField>
        <SubField label="Begriff im Dateinamen">
          <select
            value={searchPattern}
            onChange={(e) => setSearchPattern(e.target.value)}
            className="sub-select"
            style={{ minWidth: 180 }}
          >
            <option value="">alle</option>
            <BegriffeOptions terms={begriffeQ.data} />
          </select>
        </SubField>
        <SubField label="Volltext-Suche">
          <input
            type="search"
            value={searchTerm}
            placeholder="URL oder Titel enthält …"
            onChange={(e) => setSearchTerm(e.target.value)}
            className="sub-input"
            style={{ minWidth: 220 }}
          />
        </SubField>
        {(searchPattern || searchTerm) && (
          <button
            type="button"
            onClick={() => {
              setSearchPattern('');
              setSearchTerm('');
            }}
            className="self-end text-xs underline"
            style={{ color: 'var(--fg-3)' }}
          >
            Filter zurücksetzen
          </button>
        )}
      </div>

      <section
        className="mt-6 grid gap-4"
        style={{ gridTemplateColumns: 'minmax(0, 2fr) minmax(0, 1fr)' }}
      >
        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Top-Downloads
          </h2>
          <div style={{ width: '100%', height: 320 }}>
            {q.isPending ? (
              <Placeholder mode="loading" />
            ) : topBars.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <BarChart
                  data={topBars}
                  layout="vertical"
                  margin={{ top: 4, right: 12, left: 12, bottom: 0 }}
                >
                  <CartesianGrid {...cartesianGridProps} horizontal={false} vertical />
                  <XAxis type="number" {...axisProps} />
                  <YAxis dataKey="name" type="category" width={180} {...axisProps} />
                  <Tooltip contentStyle={tooltipStyle} cursor={{ fill: 'var(--surface-2)' }} />
                  <Bar dataKey="Downloads" fill={brandStroke} radius={[0, 3, 3, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Dateityp-Verteilung
          </h2>
          <div style={{ width: '100%', height: 320 }}>
            {q.isPending ? (
              <Placeholder mode="loading" />
            ) : fileTypeMix.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={fileTypeMix}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={56}
                    outerRadius={96}
                    paddingAngle={2}
                    stroke="var(--surface)"
                  >
                    {fileTypeMix.map((_, i) => (
                      <Cell key={i} fill={SERIES_PALETTE[i % SERIES_PALETTE.length]} />
                    ))}
                  </Pie>
                  <Tooltip contentStyle={tooltipStyle} />
                  <Legend wrapperStyle={legendStyle} iconType="circle" iconSize={8} />
                </PieChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </section>

      {isBrand && (
        <section className="mt-6 panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Tagesverlauf — Downloads · {brand?.label}
          </h2>
          <div style={{ width: '100%', height: 240 }}>
            {brandTimelineQ.isPending ? (
              <Placeholder mode="loading" />
            ) : brandTimelineData.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <LineChart
                  data={brandTimelineData}
                  margin={{ top: 8, right: 8, left: -8, bottom: 0 }}
                >
                  <CartesianGrid {...cartesianGridProps} />
                  <XAxis dataKey="date" {...axisProps} />
                  <YAxis {...axisProps} />
                  <Tooltip contentStyle={tooltipStyle} cursor={{ stroke: 'var(--border-strong)' }} />
                  <Line
                    type="monotone"
                    dataKey="Downloads"
                    stroke={brandStroke}
                    dot={false}
                    strokeWidth={1.5}
                  />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </section>
      )}

      <section className="mt-6">
        <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
          Alle Downloads ({rows.length})
        </h2>
        <DataTable
          rows={rows}
          columns={columns}
          rowKey={(_, i) => i}
          loading={q.isPending}
          error={q.error}
          pageSize={25}
        />
      </section>
    </div>
  );
}

function SubField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="flex flex-col text-xs">
      <span
        className="text-[10px] font-medium uppercase mb-1"
        style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
      >
        {label}
      </span>
      {children}
    </label>
  );
}

interface BegriffOption {
  term: string;
  source: string;
  count: number;
}

function BegriffeOptions({ terms }: { terms: ReadonlyArray<BegriffOption> | undefined }) {
  if (!terms || terms.length === 0) {
    return null;
  }
  const nonEmpty = terms.filter((t) => t.term && t.term.length > 0);
  const idu = nonEmpty.filter((t) => t.source === 'IDU').sort((a, b) => a.term.localeCompare(b.term));
  const to = nonEmpty.filter((t) => t.source === 'TO').sort((a, b) => a.term.localeCompare(b.term));
  return (
    <>
      {idu.length > 0 && (
        <optgroup label="Materialart (IDU)">
          {idu.map((t) => (
            <option key={`idu-${t.term}`} value={t.term}>
              {t.term} ({t.count.toLocaleString('de-AT')})
            </option>
          ))}
        </optgroup>
      )}
      {to.length > 0 && (
        <optgroup label="Themengebiet (TO)">
          {to.map((t) => (
            <option key={`to-${t.term}`} value={t.term}>
              {t.term} ({t.count.toLocaleString('de-AT')})
            </option>
          ))}
        </optgroup>
      )}
    </>
  );
}
