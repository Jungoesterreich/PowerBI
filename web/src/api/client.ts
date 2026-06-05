import type {
  Ausgabe,
  Brand,
  BrandFilter,
  BrandSummary,
  BrandTimelinePoint,
  DailyKpi,
  DailySummary,
  DownloadFilter,
  DownloadRow,
  EditionAggregate,
  MediaAudioFilter,
  MediaAudioRow,
  PageviewFilter,
  PageviewRow,
  PageviewTimelinePoint,
  RunRequest,
  RunResponse,
  SearchTerm,
  Site,
  SyncJob,
  SyncLogEntry,
  SyncStatus,
} from "./types";

const BASE_URL = import.meta.env.VITE_API_URL ?? "";

function buildQuery(params: object): string {
  const usp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === "") continue;
    usp.append(k, String(v));
  }
  const s = usp.toString();
  return s ? `?${s}` : "";
}

async function get<T>(path: string, params: object = {}): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}${buildQuery(params)}`, {
    headers: { Accept: "application/json" },
  });
  return handleResponse<T>(res);
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body ?? null),
  });
  return handleResponse<T>(res);
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text();
    const err = new Error(`API ${res.status}: ${text || res.statusText}`) as Error & {
      status?: number;
      body?: string;
    };
    err.status = res.status;
    err.body = text;
    throw err;
  }
  // The API uses PascalCase by default — System.Text.Json keeps property names
  // as-is unless we set a naming policy. We normalize to camelCase here so the
  // TypeScript types are idiomatic.
  const raw = await res.json();
  return normalize(raw) as T;
}

function normalize(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(normalize);
  if (value && typeof value === "object") {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      const key = k.length > 0 ? k[0].toLowerCase() + k.slice(1) : k;
      out[key] = normalize(v);
    }
    return out;
  }
  return value;
}

export const api = {
  daily: (params: { siteId?: number; from?: string; to?: string } = {}) =>
    get<DailyKpi[]>("/api/daily/", params),
  dailySummary: (params: { siteId?: number; from?: string; to?: string } = {}) =>
    get<DailySummary>("/api/daily/summary", params),
  pageviews: (params: PageviewFilter = {}) => get<PageviewRow[]>("/api/pageviews/", params),
  pageviewsTimeline: (params: PageviewFilter = {}) =>
    get<PageviewTimelinePoint[]>("/api/pageviews/timeline", params),
  pageviewsByEdition: (params: Omit<PageviewFilter, "ausgabe"> = {}) =>
    get<EditionAggregate[]>("/api/pageviews/by-edition", params),
  downloads: (params: DownloadFilter = {}) => get<DownloadRow[]>("/api/downloads/", params),
  mediaAudio: (params: MediaAudioFilter = {}) => get<MediaAudioRow[]>("/api/media-audio/", params),
  ausgaben: (schuljahr?: string) => get<Ausgabe[]>("/api/dim/ausgaben", { schuljahr }),
  schuljahre: () => get<string[]>("/api/dim/schuljahre"),
  sites: () => get<Site[]>("/api/dim/sites"),
  searchTerms: (siteId?: number, top?: number) =>
    get<SearchTerm[]>("/api/dim/search-terms", { siteId, top }),

  syncJobs: () => get<SyncJob[]>("/api/sync/jobs"),
  syncStatus: () => get<SyncStatus>("/api/sync/status"),
  syncLog: (params: { jobName?: string; limit?: number } = {}) =>
    get<SyncLogEntry[]>("/api/sync/log", params),
  runJob: (jobName: string, body?: RunRequest) =>
    post<RunResponse>(`/api/sync/run/${encodeURIComponent(jobName)}`, body ?? {}),
  runAll: (body?: RunRequest) => post<RunResponse>("/api/sync/run-all", body ?? {}),

  brands: () => get<Brand[]>("/api/brands/"),
  brandSummary: (key: string, params: BrandFilter = {}) =>
    get<BrandSummary>(`/api/brands/${encodeURIComponent(key)}/summary`, params),
  brandPageviews: (key: string, params: BrandFilter = {}) =>
    get<PageviewRow[]>(`/api/brands/${encodeURIComponent(key)}/pageviews`, params),
  brandDownloads: (key: string, params: BrandFilter = {}) =>
    get<DownloadRow[]>(`/api/brands/${encodeURIComponent(key)}/downloads`, params),
  brandTimeline: (key: string, params: BrandFilter = {}) =>
    get<BrandTimelinePoint[]>(`/api/brands/${encodeURIComponent(key)}/timeline`, params),
};
