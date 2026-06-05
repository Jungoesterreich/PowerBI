import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../api/client';
import PageHeader from '../components/PageHeader';
import DataTable, { type Column } from '../components/DataTable';
import type { RunRequest, SyncLogEntry } from '../api/types';
import { formatDate, truncate } from '../lib/format';

export default function Sync() {
  const queryClient = useQueryClient();
  const [override, setOverride] = useState<RunRequest>({ from: '', to: '' });
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const statusQ = useQuery({
    queryKey: ['sync', 'status'],
    queryFn: () => api.syncStatus(),
    refetchInterval: (q) => (q.state.data?.isRunning ? 1500 : 8000),
  });

  const jobsQ = useQuery({
    queryKey: ['sync', 'jobs'],
    queryFn: () => api.syncJobs(),
  });

  const logQ = useQuery({
    queryKey: ['sync', 'log'],
    queryFn: () => api.syncLog({ limit: 30 }),
    refetchInterval: () => (statusQ.data?.isRunning ? 1500 : 8000),
  });

  const runMut = useMutation({
    mutationFn: async (vars: { jobName?: string }) => {
      const body: RunRequest = {
        from: override.from || undefined,
        to: override.to || undefined,
      };
      return vars.jobName
        ? await api.runJob(vars.jobName, body)
        : await api.runAll(body);
    },
    onSuccess: (res, vars) => {
      setError(null);
      setFeedback(
        vars.jobName
          ? `Job "${vars.jobName}" gestartet (Log #${res.items[0]?.syncLogId}).`
          : `${res.items.length} Jobs gestartet.`,
      );
      queryClient.invalidateQueries({ queryKey: ['sync'] });
    },
    onError: (e: Error & { body?: string; status?: number }) => {
      setFeedback(null);
      setError(e.status === 409 ? 'Es läuft bereits ein Sync. Bitte warten.' : e.message);
    },
  });

  const running = statusQ.data?.isRunning ?? false;

  const logColumns: Column<SyncLogEntry>[] = [
    { key: 'id', label: '#', num: true, render: (r) => r.id, sortValue: (r) => r.id },
    {
      key: 'jobName',
      label: 'Job',
      render: (r) => (
        <span className="font-mono text-xs" style={{ color: 'var(--fg)' }}>
          {r.jobName}
        </span>
      ),
    },
    { key: 'startTime', label: 'Start', render: (r) => formatDate(r.startTime), sortValue: (r) => r.startTime },
    { key: 'endTime', label: 'Ende', render: (r) => formatDate(r.endTime), sortValue: (r) => r.endTime ?? '' },
    { key: 'status', label: 'Status', render: (r) => <StatusPill status={r.status} /> },
    {
      key: 'rowsAffected',
      label: 'Zeilen',
      num: true,
      render: (r) => (r.rowsAffected ?? '–'),
      sortValue: (r) => r.rowsAffected ?? 0,
    },
    {
      key: 'durationSeconds',
      label: 'Dauer',
      num: true,
      render: (r) => (r.durationSeconds === null ? '–' : `${r.durationSeconds}s`),
      sortValue: (r) => r.durationSeconds ?? 0,
    },
    {
      key: 'errorMessage',
      label: 'Fehler',
      render: (r) => (
        <span style={{ color: 'var(--neg)' }} title={r.errorMessage ?? ''}>
          {r.errorMessage ? truncate(r.errorMessage, 40) : ''}
        </span>
      ),
      sortValue: (r) => r.errorMessage ?? '',
    },
  ];

  return (
    <div>
      <PageHeader
        title="Sync"
        sub="Datenabgleich manuell auslösen (analog JoeSync.Console)"
        actions={<StatusBadge running={running} />}
      />

      {feedback && (
        <div
          className="panel panel-pad text-sm"
          style={{
            background: 'color-mix(in oklch, var(--pos) 8%, var(--surface))',
            borderColor: 'color-mix(in oklch, var(--pos) 30%, var(--border))',
            color: 'var(--fg)',
          }}
        >
          {feedback}
        </div>
      )}
      {error && (
        <div
          className="panel panel-pad text-sm mt-3"
          style={{
            background: 'color-mix(in oklch, var(--neg) 8%, var(--surface))',
            borderColor: 'color-mix(in oklch, var(--neg) 30%, var(--border))',
            color: 'var(--fg)',
          }}
        >
          {error}
        </div>
      )}

      <section className="mt-6 panel panel-pad">
        <h2 className="text-sm font-medium" style={{ color: 'var(--fg)' }}>
          Zeitraum überschreiben (optional)
        </h2>
        <p className="text-xs mt-1" style={{ color: 'var(--fg-3)' }}>
          Leer = Delta-Sync ab letztem erfolgreichen Lauf. Setzt für diesen Lauf die
          Matomo-Override-Konfiguration.
        </p>
        <div className="mt-3 flex flex-wrap items-end gap-3">
          <label className="flex flex-col text-xs">
            <span
              className="text-[10px] font-medium uppercase mb-1"
              style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
            >
              Von
            </span>
            <input
              type="date"
              value={override.from ?? ''}
              onChange={(e) => setOverride({ ...override, from: e.target.value })}
              className="sub-input"
              disabled={running}
            />
          </label>
          <label className="flex flex-col text-xs">
            <span
              className="text-[10px] font-medium uppercase mb-1"
              style={{ color: 'var(--fg-3)', letterSpacing: '0.06em' }}
            >
              Bis
            </span>
            <input
              type="date"
              value={override.to ?? ''}
              onChange={(e) => setOverride({ ...override, to: e.target.value })}
              className="sub-input"
              disabled={running}
            />
          </label>
          <button
            type="button"
            onClick={() => setOverride({ from: '', to: '' })}
            className="self-end text-xs underline disabled:opacity-50"
            style={{ color: 'var(--fg-3)' }}
            disabled={running}
          >
            zurücksetzen
          </button>
        </div>
      </section>

      <section className="mt-6 panel panel-pad">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-medium" style={{ color: 'var(--fg)' }}>
            Jobs
          </h2>
          <button
            type="button"
            onClick={() => runMut.mutate({})}
            disabled={running || runMut.isPending}
            className="px-3 py-1.5 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            style={{
              background: 'var(--fg)',
              color: 'var(--surface)',
              borderRadius: 'var(--r)',
            }}
          >
            ▶ Alle synchronisieren
          </button>
        </div>
        <div>
          {jobsQ.data?.map((j) => {
            const isCurrent = statusQ.data?.jobName === j.jobName && running;
            return (
              <div
                key={j.jobName}
                className="flex items-center justify-between py-3"
                style={{ borderTop: '1px solid var(--border)' }}
              >
                <div className="flex items-center gap-3">
                  <span
                    className={
                      isCurrent
                        ? 'inline-block w-2 h-2 rounded-full animate-pulse'
                        : 'inline-block w-2 h-2 rounded-full'
                    }
                    style={{
                      background: isCurrent ? 'var(--warn)' : 'var(--border-strong)',
                    }}
                  />
                  <span className="font-mono text-sm" style={{ color: 'var(--fg)' }}>
                    {j.jobName}
                  </span>
                  {isCurrent && (
                    <span className="text-xs" style={{ color: 'var(--warn)' }}>
                      läuft…
                    </span>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => runMut.mutate({ jobName: j.jobName })}
                  disabled={running || runMut.isPending}
                  className="text-sm px-3 py-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
                  style={{
                    background: 'var(--surface)',
                    color: 'var(--fg-2)',
                    border: '1px solid var(--border)',
                    borderRadius: 'var(--r)',
                  }}
                >
                  Jetzt synchronisieren
                </button>
              </div>
            );
          })}
          {jobsQ.data?.length === 0 && (
            <div className="text-sm py-4" style={{ color: 'var(--fg-3)' }}>
              Keine Jobs registriert.
            </div>
          )}
        </div>
      </section>

      <section className="mt-6">
        <h2 className="text-sm font-medium mb-3" style={{ color: 'var(--fg)' }}>
          Letzte Sync-Läufe
        </h2>
        <DataTable
          rows={logQ.data}
          columns={logColumns}
          rowKey={(r) => r.id}
          loading={logQ.isPending}
          error={logQ.error}
          pageSize={30}
        />
      </section>
    </div>
  );
}

function StatusBadge({ running }: { running: boolean }) {
  const color = running ? 'var(--warn)' : 'var(--pos)';
  return (
    <span
      className="inline-flex items-center gap-2 px-3 py-1 text-xs font-medium"
      style={{
        background: `color-mix(in oklch, ${color} 12%, var(--surface))`,
        border: `1px solid color-mix(in oklch, ${color} 25%, var(--border))`,
        borderRadius: 999,
        color: 'var(--fg)',
      }}
    >
      <span
        className={running ? 'w-2 h-2 rounded-full animate-pulse' : 'w-2 h-2 rounded-full'}
        style={{ background: color }}
      />
      {running ? 'Sync läuft' : 'Bereit'}
    </span>
  );
}

function StatusPill({ status }: { status: string }) {
  const color =
    status === 'Success'
      ? 'var(--pos)'
      : status === 'Running'
        ? 'var(--warn)'
        : status === 'Failed'
          ? 'var(--neg)'
          : 'var(--fg-3)';
  return (
    <span
      className="inline-flex items-center px-2 py-0.5 text-xs"
      style={{
        background: `color-mix(in oklch, ${color} 12%, var(--surface))`,
        border: `1px solid color-mix(in oklch, ${color} 22%, var(--border))`,
        borderRadius: 999,
        color: 'var(--fg)',
      }}
    >
      {status}
    </span>
  );
}

