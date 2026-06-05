import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  Treemap,
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
import type { PageviewRow } from '../api/types';

const SITE_ID = 1;

export default function Pageviews() {
  const { from, to, schuljahr, marke } = useFilters();
  const [ausgabe, setAusgabe] = useState('');
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

  const baseParams = useMemo(
    () => ({
      siteId: SITE_ID,
      from,
      to,
      schuljahr: schuljahr === 'alle' ? undefined : schuljahr,
      ausgabe: ausgabe || undefined,
      searchPattern: searchPattern || undefined,
      searchTerm: debouncedSearch || undefined,
    }),
    [from, to, schuljahr, ausgabe, searchPattern, debouncedSearch],
  );

  const topQ = useQuery({
    queryKey: ['pageviews', 'top', marke, baseParams],
    queryFn: () =>
      isBrand
        ? api.brandPageviews(marke, { ...baseParams, top: 100 })
        : api.pageviews({ ...baseParams, top: 100 }),
  });

  const timelineQ = useQuery({
    queryKey: ['pageviews', 'timeline', marke, baseParams],
    queryFn: () => api.pageviewsTimeline(baseParams),
    enabled: !isBrand,
  });

  const brandTimelineQ = useQuery({
    queryKey: ['pageviews', 'brand-timeline', marke, baseParams],
    queryFn: () => api.brandTimeline(marke, baseParams),
    enabled: isBrand,
  });

  const byEditionQ = useQuery({
    queryKey: ['pageviews', 'by-edition', marke, baseParams],
    queryFn: () =>
      api.pageviewsByEdition({
        siteId: baseParams.siteId,
        from: baseParams.from,
        to: baseParams.to,
        schuljahr: baseParams.schuljahr,
        searchPattern: baseParams.searchPattern,
        searchTerm: baseParams.searchTerm,
      }),
  });

  const timelineData = useMemo(() => {
    if (isBrand) {
      return (brandTimelineQ.data ?? []).map((p) => ({
        date: p.date.slice(0, 10),
        Hits: p.pageviews,
      }));
    }
    return (timelineQ.data ?? []).map((p) => ({
      date: p.date.slice(0, 10),
      Hits: p.hits,
      Unique: p.uniqueVisitors,
    }));
  }, [isBrand, timelineQ.data, brandTimelineQ.data]);

  const timelineLoading = isBrand ? brandTimelineQ.isPending : timelineQ.isPending;

  const editionData = useMemo(
    () =>
      (byEditionQ.data ?? []).slice(0, 25).map((e) => ({
        edition: e.edition,
        Hits: e.hits,
        Unique: e.uniqueVisitors,
      })),
    [byEditionQ.data],
  );

  const treemapData = useMemo(
    () =>
      (topQ.data ?? [])
        .slice(0, 30)
        .filter((r) => r.hits > 0)
        .map((r) => ({
          name: truncate(r.pageTitle ?? r.urlPathFull ?? r.url ?? '–', 36),
          size: r.hits,
        })),
    [topQ.data],
  );

  const brandStroke = isBrand ? BRAND_HEX[marke] : SERIES_PALETTE[1];

  const columns: Column<PageviewRow>[] = [
    {
      key: 'url',
      label: 'URL',
      render: (r) =>
        r.url ? (
          <a
            href={r.url}
            target="_blank"
            rel="noreferrer"
            className="hover:underline"
            style={{ color: 'var(--fg)' }}
          >
            {truncate(r.urlPathFull ?? r.url, 60)}
          </a>
        ) : (
          '–'
        ),
      sortValue: (r) => r.urlPathFull ?? r.url ?? '',
    },
    { key: 'pageTitle', label: 'Titel', render: (r) => truncate(r.pageTitle, 40) },
    { key: 'edition', label: 'Ausgabe', render: (r) => r.edition ?? '–' },
    { key: 'schuljahr', label: 'Schuljahr', render: (r) => r.schuljahr ?? '–' },
    { key: 'seitenart', label: 'Seitenart', render: (r) => r.seitenart ?? '–' },
    {
      key: 'searchPattern',
      label: 'SearchPattern',
      render: (r) => r.searchPatternIdu ?? r.searchPatternTo ?? '–',
      sortValue: (r) => r.searchPatternIdu ?? r.searchPatternTo ?? '',
    },
    { key: 'hits', label: 'Hits', num: true, fg: true, render: (r) => formatNumber(r.hits), sortValue: (r) => r.hits },
    {
      key: 'uniqueVisitors',
      label: 'Unique',
      num: true,
      render: (r) => formatNumber(r.uniqueVisitors),
      sortValue: (r) => r.uniqueVisitors,
    },
  ];

  return (
    <div>
      <PageHeader
        title="Pageviews"
        sub={`Seitenaufrufe ${isBrand ? `· ${brand?.label}` : 'über alle Marken'}`}
      />

      <SubFilters>
        <SubField label="Ausgabe">
          <select
            value={ausgabe}
            onChange={(e) => setAusgabe(e.target.value)}
            className="sub-select"
          >
            <option value="">alle</option>
            {ausgabenQ.data?.map((a) => (
              <option key={a.ausgabenkuerzel} value={a.ausgabenkuerzel}>
                {a.ausgabenkuerzel} ({a.schuljahr})
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
      </SubFilters>

      <section
        className="mt-6 grid gap-4"
        style={{ gridTemplateColumns: 'minmax(0, 2fr) minmax(0, 1fr)' }}
      >
        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Zeitliche Verteilung
          </h2>
          <div style={{ width: '100%', height: 280 }}>
            {timelineLoading ? (
              <Placeholder mode="loading" />
            ) : timelineData.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <LineChart data={timelineData} margin={{ top: 8, right: 8, left: -8, bottom: 0 }}>
                  <CartesianGrid {...cartesianGridProps} />
                  <XAxis dataKey="date" {...axisProps} />
                  <YAxis {...axisProps} />
                  <Tooltip contentStyle={tooltipStyle} cursor={{ stroke: 'var(--border-strong)' }} />
                  <Legend wrapperStyle={legendStyle} iconType="circle" iconSize={8} />
                  <Line
                    type="monotone"
                    dataKey="Hits"
                    stroke={brandStroke}
                    dot={false}
                    strokeWidth={1.5}
                  />
                  {!isBrand && (
                    <Line
                      type="monotone"
                      dataKey="Unique"
                      stroke={SERIES_PALETTE[2]}
                      dot={false}
                      strokeWidth={1.5}
                    />
                  )}
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Top-Ausgaben
          </h2>
          <div style={{ width: '100%', height: 280 }}>
            {byEditionQ.isPending ? (
              <Placeholder mode="loading" />
            ) : editionData.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <BarChart data={editionData} margin={{ top: 8, right: 8, left: -8, bottom: 0 }}>
                  <CartesianGrid {...cartesianGridProps} />
                  <XAxis dataKey="edition" {...axisProps} />
                  <YAxis {...axisProps} />
                  <Tooltip contentStyle={tooltipStyle} cursor={{ fill: 'var(--surface-2)' }} />
                  <Bar dataKey="Hits" fill={brandStroke} radius={[2, 2, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </section>

      <section className="mt-6 panel panel-pad">
        <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
          Treemap — Top-Seiten nach Hits
        </h2>
        <div style={{ width: '100%', height: 320 }}>
          {topQ.isPending ? (
            <Placeholder mode="loading" />
          ) : treemapData.length === 0 ? (
            <Placeholder mode="empty" />
          ) : (
            <ResponsiveContainer>
              <Treemap
                data={treemapData}
                dataKey="size"
                stroke="var(--surface)"
                fill={brandStroke}
                isAnimationActive={false}
              />
            </ResponsiveContainer>
          )}
        </div>
      </section>

      <section className="mt-6">
        <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
          Top-Seiten ({(topQ.data ?? []).length})
        </h2>
        <DataTable
          rows={topQ.data}
          columns={columns}
          rowKey={(_, i) => i}
          loading={topQ.isPending}
          error={topQ.error}
          pageSize={25}
        />
      </section>
    </div>
  );
}

function SubFilters({ children }: { children: React.ReactNode }) {
  return (
    <div
      className="flex flex-wrap items-end gap-3 mt-1"
      style={{ color: 'var(--fg-2)' }}
    >
      {children}
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
