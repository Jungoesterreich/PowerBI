export interface DailyKpi {
  date: string;
  siteName: string;
  visits: number;
  uniqueVisitors: number;
  pageViews: number;
  uniquePageViews: number;
  downloads: number;
  uniqueDownloads: number;
  events: number;
  searches: number;
  avgVisitDuration: number;
  bounceCount: number;
  bounceRate: number;
}

export interface DailySummary {
  from: string | null;
  to: string | null;
  visits: number;
  uniqueVisitors: number;
  pageViews: number;
  downloads: number;
  events: number;
  searches: number;
  bounceCount: number;
  bounceRate: number;
  avgVisitDuration: number;
  daysWithData: number;
}

export interface PageviewRow {
  url: string | null;
  pageTitle: string | null;
  urlPathFull: string | null;
  urlPathElementOne: string | null;
  urlPathElementTwo: string | null;
  edition: string | null;
  schuljahr: string | null;
  searchPatternIdu: string | null;
  searchPatternTo: string | null;
  seitenart: string | null;
  subRubrik: string | null;
  hits: number;
  uniqueVisitors: number;
}

export interface PageviewTimelinePoint {
  date: string;
  hits: number;
  uniqueVisitors: number;
}

export interface EditionAggregate {
  edition: string;
  schuljahr: string | null;
  jahr: number | null;
  ausgabe: number | null;
  hits: number;
  uniqueVisitors: number;
}

export interface DownloadRow {
  url: string | null;
  urlLastPathElement: string | null;
  fileType: string | null;
  edition: string | null;
  schuljahr: string | null;
  searchPatternIdu: string | null;
  searchPatternTo: string | null;
  downloads: number;
  uniqueDownloads: number;
}

export interface MediaAudioRow {
  label: string | null;
  url: string | null;
  urlLastPathElement: string | null;
  edition: string | null;
  schuljahr: string | null;
  plays: number;
  uniquePlays: number;
  finishes: number;
  finishRate: number;
  avgTimeWatched: number;
}

export interface Ausgabe {
  ausgabenkuerzel: string;
  schuljahr: string;
  jahr: number | null;
  ausgabe: number | null;
  ausgabeIdu: number | null;
}

export interface Site {
  idSite: number;
  siteName: string;
}

export interface SearchTerm {
  term: string;
  source: string;
  count: number;
}

export interface PageviewFilter {
  siteId?: number;
  from?: string;
  to?: string;
  schuljahr?: string;
  ausgabe?: string;
  searchTerm?: string;
  /** Exact match on SearchPattern.IDU or SearchPattern.TO — matches the PBI Begriffe-Slicer. */
  searchPattern?: string;
  top?: number;
}

export interface DownloadFilter extends PageviewFilter {
  fileType?: string;
}

export interface MediaAudioFilter {
  siteId?: number;
  from?: string;
  to?: string;
  schuljahr?: string;
  ausgabe?: string;
  top?: number;
}

export interface SyncJob {
  jobName: string;
}

export interface SyncStatus {
  isRunning: boolean;
  jobName: string | null;
  syncLogId: number | null;
  startedAt: string | null;
  overrideFrom: string | null;
  overrideTo: string | null;
}

export interface SyncLogEntry {
  id: number;
  jobName: string;
  startTime: string;
  endTime: string | null;
  status: "Unknown" | "Running" | "Success" | "Failed" | string;
  rowsAffected: number | null;
  errorMessage: string | null;
  durationSeconds: number | null;
}

export interface RunResponseItem {
  jobName: string;
  syncLogId: number;
}

export interface RunResponse {
  items: RunResponseItem[];
}

export interface RunRequest {
  from?: string;
  to?: string;
}

export interface Brand {
  key: string;
  label: string;
  urlContains: string;
  urlExcludes: string | null;
}

export interface BrandSummary {
  key: string;
  label: string;
  pageviews: number;
  uniquePageVisitors: number;
  downloads: number;
  uniqueDownloadVisitors: number;
  pages: number;
  files: number;
}

export interface BrandTimelinePoint {
  date: string;
  pageviews: number;
  downloads: number;
}

export interface BrandFilter {
  siteId?: number;
  from?: string;
  to?: string;
  schuljahr?: string;
  ausgabe?: string;
  searchTerm?: string;
  /** Exact match on SearchPattern.IDU or SearchPattern.TO. */
  searchPattern?: string;
  fileType?: string;
  top?: number;
}
