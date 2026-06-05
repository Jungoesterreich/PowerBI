/*
 * Shared Recharts theme.
 *
 * Recharts colors do not honour CSS var() inside its SVG attributes — it
 * needs literal color strings. We resolve a stable set of token-aligned
 * literals at module load. If the design tokens in tokens.css change,
 * mirror them here.
 *
 * Brand line colors are aligned with the oklch tokens but converted to
 * approximate hex equivalents because SVG stroke attrs can't reference
 * `var(--brand-xxx)` reliably across browsers when Recharts re-renders.
 */

import type { BrandKey } from './brands';

export const CHART_GRID = '#e6e4e0';        // --border
export const CHART_AXIS = '#d4d2cd';        // --border-strong
export const CHART_FG  = '#1a1a18';         // --fg
export const CHART_FG2 = '#4a4a47';         // --fg-2
export const CHART_FG3 = '#7c7b76';         // --fg-3

export const CHART_FONT_SIZE = 11;
export const CHART_FONT_FAMILY = '"Geist Mono", ui-monospace, monospace';

/** Stable brand color literals for Recharts strokes/fills. */
export const BRAND_HEX: Record<BrandKey, string> = {
  jin: '#4ca8b8',   // teal
  joe: '#5b67c9',   // indigo
  idu: '#5db282',   // green
  ms:  '#a26ec1',   // violet
  sp:  '#d49853',   // orange
  lux: '#d36a78',   // rose
  to:  '#c5a55a',   // amber
};

/** Series palette for multi-line / multi-bar charts. */
export const SERIES_PALETTE = [
  '#3f3f3c', // dark slate
  '#5b67c9', // indigo
  '#5db282', // green
  '#d49853', // orange
  '#4ca8b8', // teal
  '#a26ec1', // violet
  '#d36a78', // rose
];

export const tooltipStyle = {
  backgroundColor: '#ffffff',
  border: '1px solid var(--border-strong, #d4d2cd)',
  borderRadius: 6,
  padding: '6px 10px',
  fontSize: 12,
  boxShadow: '0 2px 4px rgba(20,20,18,0.06)',
  color: CHART_FG,
} as const;

export const axisProps = {
  tick: { fill: CHART_FG3, fontSize: CHART_FONT_SIZE, fontFamily: CHART_FONT_FAMILY },
  axisLine: { stroke: CHART_AXIS },
  tickLine: { stroke: CHART_AXIS },
} as const;

export const cartesianGridProps = {
  stroke: CHART_GRID,
  strokeDasharray: '2 4',
  vertical: false,
} as const;

export const legendStyle = {
  fontFamily: CHART_FONT_FAMILY,
  fontSize: 11,
  color: CHART_FG2,
} as const;
