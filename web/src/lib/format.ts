export function formatNumber(n: number | null | undefined): string {
  if (n === null || n === undefined) return "–";
  return new Intl.NumberFormat("de-AT").format(n);
}

export function formatPercent(n: number | null | undefined, fractionDigits = 1): string {
  if (n === null || n === undefined) return "–";
  return `${n.toFixed(fractionDigits)} %`;
}

export function formatRate(n: number | null | undefined): string {
  if (n === null || n === undefined) return "–";
  return `${(n * 100).toFixed(1)} %`;
}

export function formatDuration(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined) return "–";
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "–";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString("de-AT", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
}

export function truncate(text: string | null | undefined, max = 80): string {
  if (!text) return "–";
  return text.length > max ? `${text.slice(0, max - 1)}…` : text;
}
