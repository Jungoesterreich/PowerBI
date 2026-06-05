import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { isoDate, currentSchuljahrStart } from '../lib/dates';
import type { BrandFilter } from '../lib/brands';

export type Preset = '7T' | '30T' | '90T' | 'SJ' | 'Custom';

export interface FilterState {
  from: string;        // ISO YYYY-MM-DD
  to: string;
  schuljahr: string;   // 'alle' | '2025-2026' | …
  preset: Preset;
  marke: BrandFilter;  // global brand selection
}

interface FilterApi extends FilterState {
  setFrom: (v: string) => void;
  setTo: (v: string) => void;
  setSchuljahr: (v: string) => void;
  setMarke: (v: BrandFilter) => void;
  applyPreset: (preset: Preset) => void;
  applySchuljahr: (schuljahr: string) => void;
}

function daysAgo(n: number): string {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return isoDate(d);
}

function rangeForPreset(preset: Preset, current: { from: string; to: string }): {
  from: string;
  to: string;
} {
  const today = isoDate(new Date());
  switch (preset) {
    case '7T':
      return { from: daysAgo(6), to: today };
    case '30T':
      return { from: daysAgo(29), to: today };
    case '90T':
      return { from: daysAgo(89), to: today };
    case 'SJ': {
      const start = isoDate(currentSchuljahrStart());
      return { from: start, to: today };
    }
    case 'Custom':
    default:
      return current;
  }
}

function rangeForSchuljahr(schuljahr: string): { from: string; to: string } | null {
  // schuljahr format: "2025-2026" — runs Sep 1 (2025) through Aug 31 (2026)
  const match = /^(\d{4})-(\d{4})$/.exec(schuljahr);
  if (!match) {
    return null;
  }
  const start = `${match[1]}-09-01`;
  const end = `${match[2]}-08-31`;
  const today = isoDate(new Date());
  return { from: start, to: end < today ? end : today };
}

const FilterContext = createContext<FilterApi | null>(null);

export function FilterProvider({ children }: { children: ReactNode }) {
  const initial = useMemo<FilterState>(
    () => ({
      ...rangeForPreset('SJ', { from: '', to: '' }),
      schuljahr: 'alle',
      preset: 'SJ',
      marke: 'alle',
    }),
    [],
  );

  const [state, setState] = useState<FilterState>(initial);

  const setFrom = useCallback((from: string) => {
    setState((s) => ({ ...s, from, preset: 'Custom' }));
  }, []);

  const setTo = useCallback((to: string) => {
    setState((s) => ({ ...s, to, preset: 'Custom' }));
  }, []);

  const setSchuljahr = useCallback((schuljahr: string) => {
    setState((s) => ({ ...s, schuljahr }));
  }, []);

  const setMarke = useCallback((marke: BrandFilter) => {
    setState((s) => ({ ...s, marke }));
  }, []);

  const applyPreset = useCallback((preset: Preset) => {
    setState((s) => {
      const { from, to } = rangeForPreset(preset, { from: s.from, to: s.to });
      return { ...s, preset, from, to };
    });
  }, []);

  const applySchuljahr = useCallback((schuljahr: string) => {
    setState((s) => {
      if (schuljahr === 'alle' || schuljahr === '') {
        return { ...s, schuljahr };
      }
      const range = rangeForSchuljahr(schuljahr);
      if (!range) {
        return { ...s, schuljahr };
      }
      return { ...s, schuljahr, preset: 'SJ', ...range };
    });
  }, []);

  const value = useMemo<FilterApi>(
    () => ({
      ...state,
      setFrom,
      setTo,
      setSchuljahr,
      setMarke,
      applyPreset,
      applySchuljahr,
    }),
    [state, setFrom, setTo, setSchuljahr, setMarke, applyPreset, applySchuljahr],
  );

  return <FilterContext.Provider value={value}>{children}</FilterContext.Provider>;
}

export function useFilters(): FilterApi {
  const ctx = useContext(FilterContext);
  if (!ctx) {
    throw new Error('useFilters must be used inside <FilterProvider>.');
  }
  return ctx;
}
