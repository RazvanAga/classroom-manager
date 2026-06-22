import { BarChart3 } from "lucide-react";
import type { BehaviorBreakdown as Breakdown } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";

// The per-class behavior breakdown over the last 30 days (slice #25): headline totals plus each
// behavior's frequency and signed point contribution, most common first (the server already sorts).
// A behavior's bar is scaled to the most-frequent one so the relative frequency reads at a glance.
export function BehaviorBreakdown({
  breakdown,
  currencyIcon,
}: {
  breakdown: Breakdown;
  currencyIcon: CurrencyIcon;
}) {
  const maxCount = Math.max(1, ...breakdown.behaviors.map((b) => b.count));

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
      <header className="mb-4 flex items-center gap-2">
        <BarChart3 size={20} className="text-violet-500" />
        <h2 className="flex-1 font-extrabold text-slate-800">{ro.reports.breakdown.title}</h2>
        <span className="text-xs font-semibold text-slate-400">{ro.reports.breakdown.subtitle}</span>
      </header>

      <div className="mb-4 grid grid-cols-3 gap-2">
        <Total label={ro.reports.breakdown.awarded} value={breakdown.totalAwarded} icon={currencyIcon} />
        <Total label={ro.reports.breakdown.deducted} value={breakdown.totalDeducted} icon={currencyIcon} />
        <Total label={ro.reports.breakdown.net} value={breakdown.netPoints} icon={currencyIcon} />
      </div>

      {breakdown.behaviors.length === 0 ? (
        <p className="py-8 text-center text-sm text-slate-400">{ro.reports.breakdown.empty}</p>
      ) : (
        <ul className="space-y-2.5">
          {breakdown.behaviors.map((b) => (
            <li key={b.behaviorId} className="flex items-center gap-3">
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline justify-between gap-2">
                  <span className="truncate text-sm font-semibold text-slate-700">{b.name}</span>
                  <span className="shrink-0 text-xs font-semibold text-slate-400">
                    {ro.reports.breakdown.times(b.count)}
                  </span>
                </div>
                <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-slate-100">
                  <div
                    className={`h-full rounded-full ${b.totalPoints >= 0 ? "bg-emerald-400" : "bg-rose-400"}`}
                    style={{ width: `${(b.count / maxCount) * 100}%` }}
                  />
                </div>
              </div>
              <div className="w-16 shrink-0 text-right">
                <Points value={b.totalPoints} icon={currencyIcon} size={12} />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function Total({ label, value, icon }: { label: string; value: number; icon: CurrencyIcon }) {
  return (
    <div className="rounded-2xl bg-violet-50/70 px-3 py-2.5 text-center">
      <p className="text-[11px] font-bold uppercase tracking-wide text-slate-400">{label}</p>
      <div className="mt-0.5 flex justify-center">
        <Points value={value} icon={icon} size={13} />
      </div>
    </div>
  );
}
