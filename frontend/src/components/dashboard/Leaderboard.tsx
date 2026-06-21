import { Crown } from "lucide-react";
import type { LeaderboardEntry } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "./Points";

// All-time leaderboard, ranked by lifetime earned (issue #22 — no period tabs). The server already
// sorts by lifetime earned, then name; the top three get a medal, the rest a numbered list.
const MEDALS = ["🥇", "🥈", "🥉"];
const PODIUM = [
  "border-amber-200 bg-amber-50",
  "border-slate-200 bg-slate-50",
  "border-orange-200 bg-orange-50",
];

export function Leaderboard({
  entries,
  currencyIcon,
}: {
  entries: LeaderboardEntry[];
  currencyIcon: CurrencyIcon;
}) {
  const ranked = entries.filter((e) => e.lifetimeEarned > 0);

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
      <header className="mb-4 flex items-center gap-2">
        <Crown size={20} className="fill-amber-300 text-amber-500" />
        <h2 className="flex-1 font-extrabold text-slate-800">{ro.leaderboard.title}</h2>
        <span className="text-xs font-semibold text-slate-400">{ro.leaderboard.subtitle}</span>
      </header>

      {ranked.length === 0 ? (
        <p className="py-8 text-center text-sm text-slate-400">{ro.leaderboard.empty}</p>
      ) : (
        <ol className="space-y-1.5">
          {ranked.map((entry, i) => {
            const podium = i < 3;
            return (
              <li
                key={entry.studentId}
                className={
                  podium
                    ? `flex items-center gap-3 rounded-2xl border px-3 py-2.5 ${PODIUM[i]}`
                    : "flex items-center gap-3 rounded-xl px-3 py-1.5 hover:bg-violet-50"
                }
              >
                <span
                  className={
                    podium
                      ? "w-6 text-center text-xl"
                      : "w-6 text-right text-xs font-bold text-slate-400"
                  }
                >
                  {podium ? MEDALS[i] : i + 1}
                </span>
                <span
                  className={`flex-1 truncate ${
                    podium ? "text-sm font-bold text-slate-700" : "text-sm text-slate-600"
                  }`}
                >
                  {entry.displayName}
                </span>
                <Points
                  value={entry.lifetimeEarned}
                  icon={currencyIcon}
                  size={podium ? 13 : 11}
                  tone="neutral"
                  showSign={false}
                />
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}
