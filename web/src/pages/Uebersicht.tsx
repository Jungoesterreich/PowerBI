import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
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
import KpiCard from '../components/KpiCard';
import PageHeader from '../components/PageHeader';
import BrandSpotlight from '../components/BrandSpotlight';
import Placeholder from '../components/Placeholder';
import DataTable, { type Column } from '../components/DataTable';
import { useFilters } from '../state/filters';
import {
  formatDate,
  formatDuration,
  formatNumber,
  formatPercent,
} from '../lib/format';
import { calcDelta, prevPeriod, type Delta } from '../lib/metrics';
import {
  axisProps,
  cartesianGridProps,
  legendStyle,
  tooltipStyle,
  SERIES_PALETTE,
} from '../lib/chart-theme';
import { IconRefresh, IconUpload } from '../components/Icons';
import type { DailyKpi } from '../api/types';

const SITE_ID = 1;

export default function Uebersicht() {
  const { from, to, marke } = useFilters();
  const prev = useMemo(() => prevPeriod({ from, to }), [from, to]);

  const dailyQ = useQuery({
    queryKey: ['daily', SITE_ID, from, to],
    queryFn: () => api.daily({ siteId: SITE_ID, from, to }),
  });
  const summaryQ = useQuery({
    queryKey: ['daily', 'summary', SITE_ID, from, to],
    queryFn: () => api.dailySummary({ siteId: SITE_ID, from, to }),
  });
  const prevSummaryQ = useQuery({
    queryKey: ['daily', 'summary', SITE_ID, prev.from, prev.to],
    queryFn: () => api.dailySummary({ siteId: SITE_ID, from: prev.from, to: prev.to }),
  });

  const sparkSeries = useMemo(
    () => buildSparkSeries(dailyQ.data ?? []),
    [dailyQ.data],
  );

  const deltaOf = (
    currentKey: keyof NonNullable<typeof summaryQ.data>,
    prevKey: keyof NonNullable<typeof prevSummaryQ.data> = currentKey,
  ): Delta | undefined => {
    if (!summaryQ.data || !prevSummaryQ.data) {
      return undefined;
    }
    const cur = Number(summaryQ.data[currentKey] ?? 0);
    const pv = Number(prevSummaryQ.data[prevKey] ?? 0);
    return calcDelta(cur, pv);
  };

  const chartData = useMemo(() => {
    return (dailyQ.data ?? []).map((d) => ({
      date: d.date.slice(0, 10),
      Visits: d.visits,
      Unique: d.uniqueVisitors,
      Pageviews: d.pageViews,
      Downloads: d.downloads,
    }));
  }, [dailyQ.data]);

  return (
    <div>
      <PageHeader
        title="Übersicht"
        actions={
          <>
            <ActionButton onClick={() => dailyQ.refetch()} variant="ghost">
              <IconRefresh size={14} />
              Aktualisieren
            </ActionButton>
            <ActionButton variant="filled">
              <IconUpload size={14} />
              Export
            </ActionButton>
          </>
        }
      />

      <section className="grid gap-4 grid-cols-2 md:grid-cols-4">
        <KpiCard
          label="Visits"
          value={formatNumber(summaryQ.data?.visits)}
          delta={deltaOf('visits')}
          spark={sparkSeries.visits}
        />
        <KpiCard
          label="Unique Visitors"
          value={formatNumber(summaryQ.data?.uniqueVisitors)}
          delta={deltaOf('uniqueVisitors')}
          spark={sparkSeries.uniqueVisitors}
        />
        <KpiCard
          label="Pageviews"
          value={formatNumber(summaryQ.data?.pageViews)}
          delta={deltaOf('pageViews')}
          spark={sparkSeries.pageViews}
        />
        <KpiCard
          label="Downloads"
          value={formatNumber(summaryQ.data?.downloads)}
          delta={deltaOf('downloads')}
          spark={sparkSeries.downloads}
        />
        <KpiCard
          label="Bounce-Rate"
          value={formatPercent(summaryQ.data?.bounceRate, 1)}
          delta={deltaOf('bounceRate')}
        />
        <KpiCard
          label="Ø Besuchsdauer"
          value={formatDuration(summaryQ.data?.avgVisitDuration)}
          delta={deltaOf('avgVisitDuration')}
        />
        <KpiCard
          label="Suchen"
          value={formatNumber(summaryQ.data?.searches)}
          delta={deltaOf('searches')}
        />
        <KpiCard
          label="Tage mit Daten"
          value={formatNumber(summaryQ.data?.daysWithData)}
        />
      </section>

      <section
        className="mt-6 grid gap-4"
        style={{ gridTemplateColumns: 'minmax(0, 2fr) minmax(0, 1fr)' }}
      >
        <div className="panel panel-pad">
          <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
            Zeitverlauf
          </h2>
          <div style={{ width: '100%', height: 280 }}>
            {dailyQ.isPending ? (
              <Placeholder mode="loading" />
            ) : dailyQ.isError ? (
              <Placeholder mode="error" hint={String(dailyQ.error)} />
            ) : chartData.length === 0 ? (
              <Placeholder mode="empty" />
            ) : (
              <ResponsiveContainer>
                <LineChart data={chartData} margin={{ top: 8, right: 8, left: -8, bottom: 0 }}>
                  <CartesianGrid {...cartesianGridProps} />
                  <XAxis dataKey="date" {...axisProps} />
                  <YAxis {...axisProps} />
                  <Tooltip contentStyle={tooltipStyle} cursor={{ stroke: 'var(--border-strong)' }} />
                  <Legend wrapperStyle={legendStyle} iconType="circle" iconSize={8} />
                  <Line type="monotone" dataKey="Visits" stroke={SERIES_PALETTE[0]} dot={false} strokeWidth={1.5} />
                  <Line type="monotone" dataKey="Unique" stroke={SERIES_PALETTE[1]} dot={false} strokeWidth={1.5} />
                  <Line type="monotone" dataKey="Pageviews" stroke={SERIES_PALETTE[2]} dot={false} strokeWidth={1.5} />
                  <Line type="monotone" dataKey="Downloads" stroke={SERIES_PALETTE[3]} dot={false} strokeWidth={1.5} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <Last14DaysTable rows={dailyQ.data} loading={dailyQ.isPending} error={dailyQ.error} />
      </section>

      {marke !== 'alle' && <BrandSpotlight marke={marke} />}
    </div>
  );
}

function Last14DaysTable({
  rows,
  loading,
  error,
}: {
  rows?: DailyKpi[];
  loading: boolean;
  error: unknown;
}) {
  const data = useMemo(() => (rows ?? []).slice(-14).reverse(), [rows]);
  const columns: Column<DailyKpi>[] = [
    {
      key: 'date',
      label: 'Datum',
      render: (r) => formatDate(r.date),
      sortValue: (r) => r.date,
    },
    { key: 'visits', label: 'Visits', num: true, render: (r) => formatNumber(r.visits) },
    { key: 'pageViews', label: 'PV', num: true, render: (r) => formatNumber(r.pageViews) },
    { key: 'downloads', label: 'DL', num: true, render: (r) => formatNumber(r.downloads) },
  ];
  return (
    <div>
      <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
        Letzte 14 Tage
      </h2>
      <DataTable
        rows={data}
        columns={columns}
        rowKey={(r) => r.date}
        loading={loading}
        error={error}
        pageSize={14}
      />
    </div>
  );
}

function ActionButton({
  variant,
  onClick,
  children,
}: {
  variant: 'ghost' | 'filled';
  onClick?: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded font-medium"
      style={
        variant === 'filled'
          ? {
              background: 'var(--fg)',
              color: 'var(--surface)',
              borderRadius: 'var(--r)',
            }
          : {
              background: 'var(--surface)',
              color: 'var(--fg-2)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--r)',
            }
      }
    >
      {children}
    </button>
  );
}

interface SparkSeries {
  visits: number[];
  uniqueVisitors: number[];
  pageViews: number[];
  downloads: number[];
}

function buildSparkSeries(rows: ReadonlyArray<DailyKpi>): SparkSeries {
  return {
    visits: rows.map((r) => r.visits),
    uniqueVisitors: rows.map((r) => r.uniqueVisitors),
    pageViews: rows.map((r) => r.pageViews),
    downloads: rows.map((r) => r.downloads),
  };
}
