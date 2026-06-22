import type { TimelineDay } from "@/lib/api";
import { formatDay } from "@/lib/dates";
import { ro } from "@/lib/strings";

// A diverging daily-net bar chart for a student's point timeline (slice #25). Each day is a column:
// positive net rises above the zero baseline (emerald), negative dips below (rose). Bars are scaled to
// the largest absolute net in the window, and each carries a hover title with its date, net and the
// running balance at end of day. Pure/presentational — the parent supplies the bucketed days.

const HALF = 72; // px height of each half (above / below the baseline)

export function TimelineChart({ days }: { days: TimelineDay[] }) {
  const allFlat = days.every((d) => d.net === 0);
  if (days.length === 0 || allFlat) {
    return (
      <p className="py-12 text-center text-sm text-slate-400">{ro.reports.profile.timelineEmpty}</p>
    );
  }

  const maxNet = Math.max(1, ...days.map((d) => Math.abs(d.net)));
  const first = days[0];
  const last = days[days.length - 1];

  return (
    <div>
      <div className="flex items-stretch gap-px">
        {days.map((d) => {
          const up = d.net > 0 ? (d.net / maxNet) * HALF : 0;
          const down = d.net < 0 ? (Math.abs(d.net) / maxNet) * HALF : 0;
          const title = `${formatDay(d.date)} · ${d.net >= 0 ? "+" : ""}${d.net} · ${ro.reports.profile.balanceLabel} ${d.balance}`;
          return (
            <div key={d.date} className="group flex flex-1 flex-col" title={title}>
              <div className="flex items-end justify-center" style={{ height: HALF }}>
                {up > 0 && (
                  <div
                    style={{ height: up }}
                    className="w-full max-w-[12px] rounded-t bg-emerald-400 transition group-hover:bg-emerald-500"
                  />
                )}
              </div>
              <div className="h-px bg-slate-200" />
              <div className="flex items-start justify-center" style={{ height: HALF }}>
                {down > 0 && (
                  <div
                    style={{ height: down }}
                    className="w-full max-w-[12px] rounded-b bg-rose-400 transition group-hover:bg-rose-500"
                  />
                )}
              </div>
            </div>
          );
        })}
      </div>
      <div className="mt-2 flex justify-between text-xs font-semibold text-slate-400">
        <span>{formatDay(first.date)}</span>
        <span>{formatDay(last.date)}</span>
      </div>
    </div>
  );
}
