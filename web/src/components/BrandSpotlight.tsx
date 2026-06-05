import type { CSSProperties, ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import { findBrand, type BrandKey } from '../lib/brands';
import { useFilters } from '../state/filters';
import { formatNumber, formatPercent, truncate } from '../lib/format';

interface Props {
  marke: BrandKey;
}

const TOP_N = 5;

export default function BrandSpotlight({ marke }: Props) {
  const { from, to, schuljahr } = useFilters();
  const brand = findBrand(marke);
  const filter = {
    from,
    to,
    schuljahr: schuljahr === 'alle' ? undefined : schuljahr,
    top: TOP_N,
  };

  const pvQ = useQuery({
    queryKey: ['brand', marke, 'pageviews-top', filter],
    queryFn: () => api.brandPageviews(marke, filter),
  });

  const dlQ = useQuery({
    queryKey: ['brand', marke, 'downloads-top', filter],
    queryFn: () => api.brandDownloads(marke, filter),
  });

  // Audio top via /api/media-audio with ausgabe scope — there is no
  // brand-scoped media endpoint, but the brand URL segment can hint at
  // ausgabe. For now we fall back to the URL pattern via the standard
  // mediaAudio endpoint without ausgabe.
  const audioQ = useQuery({
    queryKey: ['brand', marke, 'audio-top', filter],
    queryFn: () => api.mediaAudio({ from, to, top: 50 }),
    select: (rows) =>
      rows
        .filter((r) => r.url?.toLowerCase().includes(marke.toLowerCase()))
        .slice(0, TOP_N),
  });

  if (!brand) {
    return null;
  }

  const accent: CSSProperties = { '--bc': `var(${brand.colorVar})` } as CSSProperties;

  return (
    <section
      className="grid gap-4 mt-6"
      style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', ...accent }}
    >
      <SpotlightCard title="Beliebteste Beiträge" loading={pvQ.isPending} accent={brand.colorVar}>
        {pvQ.data?.length
          ? pvQ.data.slice(0, TOP_N).map((row, i) => {
              const max = pvQ.data![0]?.hits ?? 1;
              return (
                <Row
                  key={`pv-${i}`}
                  label={truncate(row.pageTitle ?? row.urlPathFull, 56)}
                  sub={truncate(row.urlPathFull, 64)}
                  value={formatNumber(row.hits)}
                  fill={row.hits / max}
                />
              );
            })
          : null}
      </SpotlightCard>

      <SpotlightCard title="Häufigste Downloads" loading={dlQ.isPending} accent={brand.colorVar}>
        {dlQ.data?.length
          ? dlQ.data.slice(0, TOP_N).map((row, i) => {
              const max = dlQ.data![0]?.downloads ?? 1;
              return (
                <Row
                  key={`dl-${i}`}
                  label={truncate(row.urlLastPathElement ?? row.url, 56)}
                  sub={row.fileType ? row.fileType.toUpperCase() : '—'}
                  value={formatNumber(row.downloads)}
                  fill={row.downloads / max}
                />
              );
            })
          : null}
      </SpotlightCard>

      <SpotlightCard title="Meistgespielte Audios" loading={audioQ.isPending} accent={brand.colorVar}>
        {audioQ.data?.length
          ? audioQ.data.slice(0, TOP_N).map((row, i) => {
              const max = audioQ.data![0]?.plays ?? 1;
              const finishColor =
                row.finishRate < 0.4
                  ? 'var(--neg)'
                  : row.finishRate > 0.7
                    ? 'var(--pos)'
                    : 'var(--fg-3)';
              return (
                <Row
                  key={`au-${i}`}
                  label={truncate(row.label ?? row.urlLastPathElement, 56)}
                  sub={
                    <span style={{ color: finishColor }}>
                      Finish: {formatPercent(row.finishRate * 100, 0)}
                    </span>
                  }
                  value={formatNumber(row.plays)}
                  fill={row.plays / max}
                />
              );
            })
          : null}
      </SpotlightCard>
    </section>
  );
}

function SpotlightCard({
  title,
  loading,
  accent,
  children,
}: {
  title: string;
  loading?: boolean;
  accent: string;
  children: ReactNode;
}) {
  return (
    <div
      className="panel panel-pad"
      style={{ '--bc': `var(${accent})` } as CSSProperties}
    >
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-medium" style={{ color: 'var(--fg)' }}>
          {title}
        </h3>
        <span
          className="inline-block w-2 h-2 rounded-full"
          style={{ background: 'var(--bc)' }}
          aria-hidden="true"
        />
      </div>
      {loading ? (
        <div className="text-xs" style={{ color: 'var(--fg-3)' }}>
          Lade …
        </div>
      ) : (
        <div className="flex flex-col gap-2">{children}</div>
      )}
    </div>
  );
}

function Row({
  label,
  sub,
  value,
  fill,
}: {
  label: ReactNode;
  sub?: ReactNode;
  value: ReactNode;
  fill: number;
}) {
  const pct = Math.max(0, Math.min(1, fill));
  return (
    <div
      className="relative px-2 py-1.5"
      style={{ borderRadius: 'var(--r)', overflow: 'hidden' }}
    >
      <div
        aria-hidden="true"
        style={{
          position: 'absolute',
          inset: 0,
          background: 'color-mix(in oklch, var(--bc) 12%, transparent)',
          width: `${pct * 100}%`,
          transition: 'width 200ms',
        }}
      />
      <div className="relative flex items-center justify-between gap-3">
        <div className="min-w-0">
          <div
            className="text-sm truncate"
            style={{ color: 'var(--fg)' }}
          >
            {label}
          </div>
          {sub && (
            <div
              className="text-[11px] truncate"
              style={{ color: 'var(--fg-3)', fontFamily: '"Geist Mono", monospace' }}
            >
              {sub}
            </div>
          )}
        </div>
        <div className="num text-sm" style={{ color: 'var(--fg)', fontWeight: 500 }}>
          {value}
        </div>
      </div>
    </div>
  );
}
