export function isoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function currentSchuljahrStart(today: Date = new Date()): Date {
  // Schuljahr in Österreich beginnt am 1. September.
  const year = today.getFullYear();
  const sept = new Date(year, 8, 1);
  return today >= sept ? sept : new Date(year - 1, 8, 1);
}

export function defaultRange(): { from: string; to: string } {
  const today = new Date();
  return { from: isoDate(currentSchuljahrStart(today)), to: isoDate(today) };
}
