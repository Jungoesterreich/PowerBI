import { useMemo, useState } from 'react';
import { useQueries, useQuery } from '@tanstack/react-query';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { api } from '../api/client';
import PageHeader from '../components/PageHeader';
import KpiCard from '../components/KpiCard';
import Placeholder from '../components/Placeholder';
import DataTable, { type Column } from '../components/DataTable';
import { useFilters } from '../state/filters';
import { BRANDS, findBrand, type BrandKey } from '../lib/brands';
import { formatNumber, truncate } from '../lib/format';
import {
  axisProps,
  cartesianGridProps,
  legendStyle,
  tooltipStyle,
  BRAND_HEX,
} from '../lib/chart-theme';
import type { BrandTimelinePoint, DownloadRow, PageviewRow } from '../api/types';
import type { CSSProperties } from 'react';

const SITE_ID = 1;

export default function Heftreihen() {
  const { from, to, schuljahr, marke, setMarke } = useFilters();
  const [ausgabe, setAusgabe] = useState('');

  const isBrand = marke !== 'alle';
  const brand = findBrand(marke);

  const ausgabenQ = useQuery({
    queryKey: ['dim', 'ausgaben', schuljahr],
    queryFn: () => api.ausgaben(schuljahr === 'alle' ? undefined : schuljahr),
  });

  const params = useMemo(
    () => ({
      siteId: SITE_ID,
      from,
      to,
      schuljahr: schuljahr === 'alle' ? undefined : schuljahr,
      ausgabe: ausgabe || undefined,
    }),
    [from, to, schuljahr, ausgabe],
  );

  return (
    <div>
      <PageHeader
        title="Heftreihen"
        sub={
          isBrand
            ? `Markenspezifische KPIs · ${brand?.label}`
            : 'Vergleich aller Marken — eine Marke oben auswählen für Detailansicht'
        }
      />

      <div className="flex flex-wrap items-end gap-3 mt-1">
        <label className="flex flex-col text-xs">
          <span
            className="text-[10px] font-medium uppercase mb-1"
            style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
          >
            Ausgabe
          </span>
          <select value={ausgabe} onChange={(e) => setAusgabe(e.target.value)} className="sub-select">
            <option value="">alle</option>
            {ausgabenQ.data?.map((a) => (
              <option key={a.ausgabenkuerzel} value={a.ausgabenkuerzel}>
                {a.ausgabenkuerzel} ({a.schuljahr})
              </option>
            ))}
          </select>
        </label>
      </div>

      {isBrand ? (
        <BrandDetail brandKey={marke} params={params} />
      ) : (
        <CompareStrip params={params} onPickBrand={setMarke} />
      )}
    </div>
  );
}

interface ParamsT {
  siteId: number;
  from: string;
  to: string;
  schuljahr: string | undefined;
  ausgabe: string | undefined;
}

function CompareStrip({
  params,
  onPickBrand,
}: {
  params: ParamsT;
  onPickBrand: (key: BrandKey) => void;
}) {
  const results = useQueries({
    queries: BRANDS.map((b) => ({
      queryKey: ['brand', 'summary', b.key, params],
      queryFn: () => api.brandSummary(b.key, params),
    })),
  });

  return (
    <section
      className="mt-6 grid gap-3"
      style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))' }}
    >
      {BRANDS.map((b, i) => {
        const r = results[i];
        const def = b;
        const style: CSSProperties = { '--bc': `var(${def.colorVar})` } as CSSProperties;
        return (
          <button
            key={b.key}
            type="button"
            onClick={() => onPickBrand(b.key)}
            className="panel panel-pad text-left transition-colors"
            style={{
              ...style,
              borderTopWidth: 3,
              borderTopColor: 'var(--bc)',
              cursor: 'pointer',
            }}
          >
            <div className="flex items-baseline justify-between">
              <span
                className="font-mono uppercase text-[10px] px-1.5 py-0.5"
                style={{
                  background: 'color-mix(in oklch, var(--bc) 18%, var(--surface))',
                  color: 'var(--fg)',
                  borderRadius: 3,
                }}
              >
                {def.shortId}
              </span>
              <span className="text-xs" style={{ color: 'var(--fg-3)' }}>
                Details →
              </span>
            </div>
            <div
              className="mt-2 text-base"
              style={{ color: 'var(--fg)', fontWeight: 500 }}
            >
              {def.label}
            </div>
            <div className="mt-3 grid grid-cols-2 gap-2">
              <Mini label="Pageviews" value={r.data?.pageviews} loading={r.isPending} />
              <Mini label="Downloads" value={r.data?.downloads} loading={r.isPending} />
              <Mini label="Seiten" value={r.data?.pages} loading={r.isPending} />
              <Mini label="Dateien" value={r.data?.files} loading={r.isPending} />
            </div>
          </button>
        );
      })}
    </section>
  );
}

function Mini({
  label,
  value,
  loading,
}: {
  label: string;
  value: number | undefined;
  loading: boolean;
}) {
  return (
    <div>
      <div
        className="text-[10px] uppercase"
        style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
      >
        {label}
      </div>
      <div className="num text-sm" style={{ color: 'var(--fg)', fontWeight: 500 }}>
        {loading ? '…' : formatNumber(value)}
      </div>
    </div>
  );
}

function BrandDetail({
  brandKey,
  params,
}: {
  brandKey: BrandKey;
  params: ParamsT;
}) {
  const stroke = BRAND_HEX[brandKey];

  const summaryQ = useQuery({
    queryKey: ['brand', 'summary', brandKey, params],
    queryFn: () => api.brandSummary(brandKey, params),
  });

  const timelineQ = useQuery({
    queryKey: ['brand', 'timeline', brandKey, params],
    queryFn: () => api.brandTimeline(brandKey, params),
  });

  const pageviewsQ = useQuery({
    queryKey: ['brand', 'pageviews', brandKey, params],
    queryFn: () => api.brandPageviews(brandKey, { ...params, top: 50 }),
  });

  const downloadsQ = useQuery({
    queryKey: ['brand', 'downloads', brandKey, params],
    queryFn: () => api.brandDownloads(brandKey, { ...params, top: 50 }),
  });

  const timelineData = useMemo(
    () =>
      (timelineQ.data ?? []).map((p) => ({
        date: p.date.slice(0, 10),
        Pageviews: p.pageviews,
        Downloads: p.downloads,
      })),
    [timelineQ.data],
  );

  const pvColumns: Column<PageviewRow>[] = [
    {
      key: 'url',
      label: 'Seite',
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
            {truncate(r.pageTitle ?? r.urlPathFull ?? r.url, 60)}
          </a>
        ) : (
          '–'
        ),
      sortValue: (r) => r.urlPathFull ?? r.url ?? '',
    },
    { key: 'edition', label: 'Ausgabe', render: (r) => r.edition ?? '–' },
    {
      key: 'searchPattern',
      label: 'SearchPattern',
      render: (r) => r.searchPatternIdu ?? r.searchPatternTo ?? '–',
    },
    { key: 'hits', label: 'Hits', num: true, fg: true, render: (r) => formatNumber(r.hits), sortValue: (r) => r.hits },
    { key: 'uniqueVisitors', label: 'Unique', num: true, render: (r) => formatNumber(r.uniqueVisitors), sortValue: (r) => r.uniqueVisitors },
  ];

  const dlColumns: Column<DownloadRow>[] = [
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
            {truncate(r.urlLastPathElement ?? r.url, 60)}
          </a>
        ) : (
          '–'
        ),
      sortValue: (r) => r.urlLastPathElement ?? r.url ?? '',
    },
    { key: 'fileType', label: 'Typ', render: (r) => (r.fileType ?? '–').toUpperCase() },
    { key: 'edition', label: 'Ausgabe', render: (r) => r.edition ?? '–' },
    { key: 'downloads', label: 'Downloads', num: true, fg: true, render: (r) => formatNumber(r.downloads), sortValue: (r) => r.downloads },
    { key: 'uniqueDownloads', label: 'Unique', num: true, render: (r) => formatNumber(r.uniqueDownloads), sortValue: (r) => r.uniqueDownloads },
  ];

  return (
    <>
      <section className="mt-6 grid grid-cols-2 md:grid-cols-4 gap-4">
        <KpiCard label="Pageviews" value={formatNumber(summaryQ.data?.pageviews)} brandColor={stroke} />
        <KpiCard label="Unique (Pages)" value={formatNumber(summaryQ.data?.uniquePageVisitors)} />
        <KpiCard label="Downloads" value={formatNumber(summaryQ.data?.downloads)} brandColor={stroke} />
        <KpiCard label="Unique (DL)" value={formatNumber(summaryQ.data?.uniqueDownloadVisitors)} />
        <KpiCard label="Distinkte Seiten" value={formatNumber(summaryQ.data?.pages)} />
        <KpiCard label="Distinkte Dateien" value={formatNumber(summaryQ.data?.files)} />
      </section>

      <section
        className="mt-6 grid gap-4"
        style={{ gridTemplateColumns: 'minmax(0, 2fr) minmax(0, 1fr)' }}
      >
        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Tagesverlauf — Pageviews & Downloads
          </h2>
          <div style={{ width: '100%', height: 280 }}>
            {timelineQ.isPending ? (
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
                  <Line type="monotone" dataKey="Pageviews" stroke={stroke} dot={false} strokeWidth={1.5} />
                  <Line type="monotone" dataKey="Downloads" stroke="var(--fg-3)" dot={false} strokeWidth={1.5} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Wochen-Heatmap
          </h2>
          <p className="text-xs mb-3" style={{ color: 'var(--fg-3)' }}>
            Pageviews je Wochentag × Kalenderwoche.
          </p>
          {timelineQ.isPending ? (
            <Placeholder mode="loading" />
          ) : timelineData.length === 0 ? (
            <Placeholder mode="empty" />
          ) : (
            <WeeklyHeatmap data={timelineQ.data ?? []} accent={stroke} />
          )}
        </div>
      </section>

      <section className="mt-6 grid grid-cols-1 xl:grid-cols-2 gap-4">
        <div>
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Top Downloads
          </h2>
          <DataTable
            rows={downloadsQ.data}
            columns={dlColumns}
            rowKey={(_, i) => i}
            loading={downloadsQ.isPending}
            error={downloadsQ.error}
            pageSize={15}
          />
        </div>
        <div>
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Top Pageviews
          </h2>
          <DataTable
            rows={pageviewsQ.data}
            columns={pvColumns}
            rowKey={(_, i) => i}
            loading={pageviewsQ.isPending}
            error={pageviewsQ.error}
            pageSize={15}
          />
        </div>
      </section>
    </>
  );
}

const WEEKDAYS = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

function isoWeek(d: Date): string {
  const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  const dayNum = date.getUTCDay() || 7;
  date.setUTCDate(date.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil(((date.getTime() - yearStart.getTime()) / 86_400_000 + 1) / 7);
  return `${date.getUTCFullYear()}-W${String(weekNo).padStart(2, '0')}`;
}

function WeeklyHeatmap({
  data,
  accent,
}: {
  data: ReadonlyArray<BrandTimelinePoint>;
  accent: string;
}) {
  const grid = useMemo(() => {
    const weeks = new Map<string, Array<number | null>>();
    let max = 0;
    for (const p of data) {
      const d = new Date(p.date);
      const wk = isoWeek(d);
      const dayIdx = ((d.getDay() + 6) % 7); // Mo=0
      const cells = weeks.get(wk) ?? new Array<number | null>(7).fill(null);
      cells[dayIdx] = p.pageviews;
      weeks.set(wk, cells);
      if (p.pageviews > max) {
        max = p.pageviews;
      }
    }
    return {
      weeks: Array.from(weeks.entries()).sort(([a], [b]) => a.localeCompare(b)),
      max,
    };
  }, [data]);

  if (grid.weeks.length === 0) {
    return <Placeholder mode="empty" />;
  }

  const cellSize = 14;
  const gap = 3;

  return (
    <div
      className="overflow-x-auto"
      style={{ '--accent': accent } as CSSProperties}
    >
      <div className="flex" style={{ gap }}>
        <div
          className="flex flex-col justify-between text-[10px]"
          style={{
            color: 'var(--fg-3)',
            fontFamily: '"Geist Mono", monospace',
            paddingTop: 14 + gap,
          }}
        >
          {WEEKDAYS.map((d) => (
            <div key={d} style={{ height: cellSize, lineHeight: `${cellSize}px` }}>
              {d}
            </div>
          ))}
        </div>
        <div>
          <div
            className="flex text-[10px]"
            style={{
              color: 'var(--fg-3)',
              fontFamily: '"Geist Mono", monospace',
              gap,
              marginBottom: gap,
            }}
          >
            {grid.weeks.map(([wk]) => (
              <div
                key={wk}
                style={{ width: cellSize, textAlign: 'center' }}
                title={wk}
              >
                {wk.slice(-2)}
              </div>
            ))}
          </div>
          <div className="flex" style={{ gap }}>
            {grid.weeks.map(([wk, cells]) => (
              <div key={wk} className="flex flex-col" style={{ gap }}>
                {cells.map((v, i) => {
                  const ratio = v === null || grid.max === 0 ? 0 : v / grid.max;
                  const opacity = v === null ? 0 : 0.12 + ratio * 0.88;
                  return (
                    <div
                      key={i}
                      title={v === null ? `${wk} ${WEEKDAYS[i]} — keine Daten` : `${wk} ${WEEKDAYS[i]}: ${formatNumber(v)}`}
                      style={{
                        width: cellSize,
                        height: cellSize,
                        borderRadius: 2,
                        background:
                          v === null
                            ? 'var(--surface-2)'
                            : `color-mix(in oklch, var(--accent) ${Math.round(opacity * 100)}%, var(--surface))`,
                        border: '1px solid var(--border)',
                      }}
                    />
                  );
                })}
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
