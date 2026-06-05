import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  CartesianGrid,
  ReferenceArea,
  ReferenceLine,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
  ZAxis,
} from 'recharts';
import { api } from '../api/client';
import PageHeader from '../components/PageHeader';
import Placeholder from '../components/Placeholder';
import DataTable, { type Column } from '../components/DataTable';
import { useFilters } from '../state/filters';
import { findBrand } from '../lib/brands';
import { formatDuration, formatNumber, formatRate, truncate } from '../lib/format';
import {
  axisProps,
  cartesianGridProps,
  tooltipStyle,
  BRAND_HEX,
  SERIES_PALETTE,
} from '../lib/chart-theme';
import type { MediaAudioRow } from '../api/types';

const SITE_ID = 1;

export default function MediaAudio() {
  const { from, to, schuljahr, marke } = useFilters();
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

  const q = useQuery({
    queryKey: ['media-audio', params],
    queryFn: () => api.mediaAudio({ ...params, top: 200 }),
  });

  const allRows = q.data ?? [];

  // Brand routing is client-side here (no /api/brands/{key}/audio endpoint).
  const rows = useMemo(() => {
    if (!isBrand) {
      return allRows;
    }
    const needle = marke.toLowerCase();
    return allRows.filter((r) => (r.url ?? '').toLowerCase().includes(needle));
  }, [allRows, isBrand, marke]);

  const scatterData = useMemo(
    () =>
      rows
        .filter((r) => r.plays > 0)
        .map((r) => ({
          label: truncate(r.label ?? r.urlLastPathElement ?? r.url ?? '–', 40),
          plays: r.plays,
          finishRate: Math.round(r.finishRate * 100),
          avg: r.avgTimeWatched,
        })),
    [rows],
  );

  const dotColor = isBrand ? BRAND_HEX[marke] : SERIES_PALETTE[4];

  const xMax = useMemo(
    () => scatterData.reduce((m, p) => Math.max(m, p.plays), 0),
    [scatterData],
  );

  const columns: Column<MediaAudioRow>[] = [
    {
      key: 'label',
      label: 'Titel / Datei',
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
            {truncate(r.label ?? r.urlLastPathElement ?? r.url, 70)}
          </a>
        ) : (
          truncate(r.label, 70)
        ),
      sortValue: (r) => r.label ?? r.urlLastPathElement ?? r.url ?? '',
    },
    { key: 'edition', label: 'Ausgabe', render: (r) => r.edition ?? '–' },
    { key: 'schuljahr', label: 'Schuljahr', render: (r) => r.schuljahr ?? '–' },
    { key: 'plays', label: 'Plays', num: true, fg: true, render: (r) => formatNumber(r.plays), sortValue: (r) => r.plays },
    {
      key: 'uniquePlays',
      label: 'Unique',
      num: true,
      render: (r) => formatNumber(r.uniquePlays),
      sortValue: (r) => r.uniquePlays,
    },
    {
      key: 'finishes',
      label: 'Finishes',
      num: true,
      render: (r) => formatNumber(r.finishes),
      sortValue: (r) => r.finishes,
    },
    {
      key: 'finishRate',
      label: 'Finish-Rate',
      num: true,
      render: (r) => formatRate(r.finishRate),
      sortValue: (r) => r.finishRate,
    },
    {
      key: 'avgTimeWatched',
      label: 'Ø Watched',
      num: true,
      render: (r) => formatDuration(r.avgTimeWatched),
      sortValue: (r) => r.avgTimeWatched,
    },
  ];

  return (
    <div>
      <PageHeader
        title="Audio & Video"
        sub={`Wiedergaben und Abschlussrate ${isBrand ? `· ${brand?.label}` : 'über alle Marken'}`}
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

      <section className="mt-6 panel panel-pad">
        <div className="flex items-baseline justify-between mb-3">
          <h2 className="text-sm font-medium" style={{ color: 'var(--fg)' }}>
            Plays vs. Finish-Rate
          </h2>
          <span className="text-xs" style={{ color: 'var(--fg-3)' }}>
            Quadranten: rechts oben = Hits · links unten = Schwach
          </span>
        </div>
        <div style={{ width: '100%', height: 360 }}>
          {q.isPending ? (
            <Placeholder mode="loading" />
          ) : scatterData.length === 0 ? (
            <Placeholder mode="empty" />
          ) : (
            <ResponsiveContainer>
              <ScatterChart margin={{ top: 8, right: 16, left: 0, bottom: 8 }}>
                <CartesianGrid {...cartesianGridProps} />
                <XAxis
                  type="number"
                  dataKey="plays"
                  name="Plays"
                  domain={[0, 'dataMax']}
                  {...axisProps}
                />
                <YAxis
                  type="number"
                  dataKey="finishRate"
                  name="Finish %"
                  unit="%"
                  domain={[0, 100]}
                  {...axisProps}
                />
                <ZAxis range={[60, 60]} />
                <ReferenceArea
                  x1={xMax / 2}
                  x2={xMax}
                  y1={50}
                  y2={100}
                  fill="var(--pos)"
                  fillOpacity={0.04}
                  stroke="none"
                />
                <ReferenceArea
                  x1={0}
                  x2={xMax / 2}
                  y1={0}
                  y2={50}
                  fill="var(--neg)"
                  fillOpacity={0.04}
                  stroke="none"
                />
                <ReferenceLine x={xMax / 2} stroke="var(--border-strong)" strokeDasharray="2 4" />
                <ReferenceLine y={50} stroke="var(--border-strong)" strokeDasharray="2 4" />
                <Tooltip
                  contentStyle={tooltipStyle}
                  cursor={{ strokeDasharray: '2 4' }}
                  formatter={(value: number | string, name: string) => {
                    if (name === 'Finish %') {
                      return [`${value} %`, name];
                    }
                    return [formatNumber(Number(value)), name];
                  }}
                  labelFormatter={(_, payload) => {
                    const p = payload?.[0]?.payload as { label?: string } | undefined;
                    return p?.label ?? '';
                  }}
                />
                <Scatter
                  data={scatterData}
                  fill={dotColor}
                  fillOpacity={0.75}
                  stroke={dotColor}
                />
              </ScatterChart>
            </ResponsiveContainer>
          )}
        </div>
      </section>

      <section className="mt-6">
        <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
          Top-Medien ({rows.length})
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
