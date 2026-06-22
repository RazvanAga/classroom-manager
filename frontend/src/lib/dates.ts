// Romanian date formatting for the reports views (slice #25). Timeline dates are yyyy-MM-dd — a local
// calendar day with no time — so they must be parsed as *local* midnight, not UTC, or a negative-offset
// zone would shift them back a day.

const dayFormat = new Intl.DateTimeFormat("ro-RO", { day: "numeric", month: "short" });
const dateTimeFormat = new Intl.DateTimeFormat("ro-RO", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

// A Date → yyyy-MM-dd using its *local* components (the form the reporting API expects).
export function ymd(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function formatDay(isoDay: string): string {
  const [y, m, d] = isoDay.split("-").map(Number);
  return dayFormat.format(new Date(y, m - 1, d));
}

export function formatDateTime(iso: string): string {
  return dateTimeFormat.format(new Date(iso));
}
