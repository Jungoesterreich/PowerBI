import { useMemo, useState, type ReactNode } from 'react';
import clsx from 'clsx';
import Placeholder from './Placeholder';

export interface Column<T> {
  key: string;
  label: string;
  /** Right-align + mono font for numbers. */
  num?: boolean;
  /** Primary foreground (var(--fg)) instead of secondary. */
  fg?: boolean;
  /** Width hint. CSS-grid-friendly string ("min-content", "1fr", "120px"). */
  width?: string;
  /** Custom cell renderer. If omitted, the column key is read off the row. */
  render?: (row: T) => ReactNode;
  /** Custom value provider for sorting. Defaults to render output or row[key]. */
  sortValue?: (row: T) => number | string | null;
  /** Disable sorting on this column. */
  sortable?: false;
}

interface Props<T> {
  rows: ReadonlyArray<T> | null | undefined;
  columns: ReadonlyArray<Column<T>>;
  pageSize?: number;
  loading?: boolean;
  error?: unknown;
  /** Stable React key per row. */
  rowKey: (row: T, index: number) => string | number;
  /** Optional placeholder when there are no rows. */
  emptyHint?: ReactNode;
}

type SortState = { key: string; dir: 'asc' | 'desc' } | null;

export default function DataTable<T>({
  rows,
  columns,
  pageSize = 25,
  loading,
  error,
  rowKey,
  emptyHint,
}: Props<T>) {
  const [sort, setSort] = useState<SortState>(null);
  const [showAll, setShowAll] = useState(false);

  const sorted = useMemo(() => {
    if (!rows || !sort) {
      return rows ?? [];
    }
    const col = columns.find((c) => c.key === sort.key);
    if (!col) {
      return rows;
    }
    const getValue =
      col.sortValue ??
      ((r: T): number | string | null => {
        const v = (r as Record<string, unknown>)[col.key];
        if (typeof v === 'number' || typeof v === 'string') {
          return v;
        }
        return v == null ? null : String(v);
      });
    const dir = sort.dir === 'asc' ? 1 : -1;
    return [...rows].sort((a, b) => {
      const av = getValue(a);
      const bv = getValue(b);
      if (av === bv) {
        return 0;
      }
      if (av === null || av === undefined) {
        return 1;
      }
      if (bv === null || bv === undefined) {
        return -1;
      }
      if (typeof av === 'number' && typeof bv === 'number') {
        return (av - bv) * dir;
      }
      return String(av).localeCompare(String(bv)) * dir;
    });
  }, [rows, sort, columns]);

  const visible = showAll ? sorted : sorted.slice(0, pageSize);

  if (loading) {
    return (
      <div className="panel">
        <Placeholder mode="loading" />
      </div>
    );
  }
  if (error) {
    return (
      <div className="panel">
        <Placeholder mode="error" hint={String((error as Error)?.message ?? error)} />
      </div>
    );
  }
  if (!sorted || sorted.length === 0) {
    return (
      <div className="panel">
        <Placeholder mode="empty" hint={emptyHint} />
      </div>
    );
  }

  const toggleSort = (key: string) => {
    const col = columns.find((c) => c.key === key);
    if (!col || col.sortable === false) {
      return;
    }
    setSort((s) => {
      if (!s || s.key !== key) {
        return { key, dir: col.num ? 'desc' : 'asc' };
      }
      return s.dir === 'asc' ? { key, dir: 'desc' } : null;
    });
  };

  return (
    <div className="panel overflow-hidden">
      <table className="min-w-full">
        <thead className="table-head">
          <tr>
            {columns.map((c) => {
              const isSorted = sort?.key === c.key;
              const sortable = c.sortable !== false;
              return (
                <th
                  key={c.key}
                  className={clsx('table-cell select-none', c.num && 'text-right', sortable && 'cursor-pointer')}
                  onClick={sortable ? () => toggleSort(c.key) : undefined}
                  style={c.width ? { width: c.width } : undefined}
                >
                  <span className="inline-flex items-center gap-1">
                    {c.label}
                    {isSorted && (
                      <span aria-hidden="true" style={{ color: 'var(--fg)' }}>
                        {sort?.dir === 'asc' ? '↑' : '↓'}
                      </span>
                    )}
                  </span>
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody>
          {visible.map((row, i) => (
            <tr
              key={rowKey(row, i)}
              style={{ borderTop: '1px solid var(--border)' }}
              className="hover:bg-[var(--surface-2)]"
            >
              {columns.map((c) => {
                const v = c.render ? c.render(row) : (row as Record<string, unknown>)[c.key];
                return (
                  <td
                    key={c.key}
                    className={clsx('table-cell', c.num && 'text-right num')}
                    style={{ color: c.fg ? 'var(--fg)' : 'var(--fg-2)' }}
                  >
                    {v as ReactNode}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
      {sorted.length > pageSize && (
        <div
          className="flex items-center justify-between px-3 py-2 text-xs"
          style={{
            borderTop: '1px solid var(--border)',
            background: 'var(--surface-2)',
            color: 'var(--fg-3)',
          }}
        >
          <span>
            {visible.length} von {sorted.length}
          </span>
          <button
            type="button"
            onClick={() => setShowAll((s) => !s)}
            className="text-xs font-medium px-2 py-1 rounded"
            style={{
              background: 'var(--surface)',
              border: '1px solid var(--border)',
              color: 'var(--fg-2)',
            }}
          >
            {showAll ? 'Weniger anzeigen' : 'Mehr anzeigen'}
          </button>
        </div>
      )}
    </div>
  );
}
