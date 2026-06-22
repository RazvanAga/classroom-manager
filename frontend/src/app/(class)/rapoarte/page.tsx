"use client";

import { useQuery } from "@tanstack/react-query";
import { Loader2, Users } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useMemo, useState } from "react";
import {
  fetchBehaviorBreakdown,
  fetchClasses,
  fetchLeaderboard,
  fetchStudents,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ymd } from "@/lib/dates";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";
import { BehaviorBreakdown } from "@/components/reports/BehaviorBreakdown";
import { StudentProfile } from "@/components/reports/StudentProfile";

// Rapoarte (issue #25): read-only class analytics. The landing view shows the per-class behavior
// breakdown plus a student picker; choosing a student opens their full report profile (stats, a
// timeline chart, and the paginated event history). Class-scoped via ?class=, like every section.
export default function RapoartePage() {
  const classId = useSearchParams().get("class") ?? "";
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // The breakdown defaults to the last 30 days (matches the server default and the UI subtitle).
  const { from, to } = useMemo(() => {
    const now = new Date();
    const start = new Date(now);
    start.setDate(start.getDate() - 29);
    return { from: ymd(start), to: ymd(now) };
  }, []);

  const classes = useQuery({ queryKey: ["classes"], queryFn: fetchClasses });
  const students = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
    enabled: !!classId,
  });
  const leaderboard = useQuery({
    queryKey: ["leaderboard", classId],
    queryFn: () => fetchLeaderboard(classId),
    enabled: !!classId,
  });
  const breakdown = useQuery({
    queryKey: ["breakdown", classId, from, to],
    queryFn: () => fetchBehaviorBreakdown(classId, from, to),
    enabled: !!classId,
  });

  const currencyIcon: CurrencyIcon =
    classes.data?.find((c) => c.id === classId)?.currencyIcon ?? "Star";

  const balanceByStudent = useMemo(() => {
    const map = new Map<string, { wallet: number; lifetimeEarned: number }>();
    for (const e of leaderboard.data ?? []) {
      map.set(e.studentId, { wallet: e.wallet, lifetimeEarned: e.lifetimeEarned });
    }
    return map;
  }, [leaderboard.data]);

  const roster = useMemo(
    () => [...(students.data ?? [])].sort((a, b) => a.displayName.localeCompare(b.displayName)),
    [students.data],
  );

  const selected = roster.find((s) => s.id === selectedId) ?? null;

  if (students.isLoading || leaderboard.isLoading) {
    return (
      <div className="grid place-items-center py-24 text-indigo-900/60">
        <Loader2 className="animate-spin" size={28} />
      </div>
    );
  }

  if (selected) {
    const balance = balanceByStudent.get(selected.id) ?? { wallet: 0, lifetimeEarned: 0 };
    return (
      <StudentProfile
        classId={classId}
        studentId={selected.id}
        studentName={selected.displayName}
        wallet={balance.wallet}
        lifetimeEarned={balance.lifetimeEarned}
        currencyIcon={currencyIcon}
        onBack={() => setSelectedId(null)}
      />
    );
  }

  if (roster.length === 0) {
    return (
      <div className="rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-16 text-center">
        <Users size={40} className="mx-auto text-violet-300" />
        <p className="mt-3 text-lg font-bold text-slate-600">{ro.reports.emptyRoster}</p>
        <p className="mt-1 text-sm text-slate-400">
          <Link href={`/setari?class=${classId}`} className="font-semibold text-violet-600 underline">
            {ro.dashboard.emptyRosterHint}
          </Link>
        </p>
      </div>
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
      <div className="space-y-4">
        <p className="text-sm font-semibold text-slate-500">{ro.reports.pickStudent}</p>
        <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 lg:grid-cols-5">
          {roster.map((s) => {
            const balance = balanceByStudent.get(s.id);
            return (
              <button
                key={s.id}
                type="button"
                onClick={() => setSelectedId(s.id)}
                className="flex cursor-pointer flex-col items-center gap-1.5 rounded-2xl border-2 border-violet-200 bg-white p-2 pt-3 transition-all duration-150 hover:scale-105 hover:border-violet-400 hover:shadow-lg"
              >
                <StudentAvatar classId={classId} studentId={s.id} size={56} />
                <span className="line-clamp-2 px-0.5 text-center text-xs font-bold leading-tight text-slate-700">
                  {s.displayName.split(" ")[0]}
                </span>
                <Points
                  value={balance?.wallet ?? 0}
                  icon={currencyIcon}
                  size={11}
                  tone="neutral"
                  showSign={false}
                />
              </button>
            );
          })}
        </div>
      </div>

      <div>
        {breakdown.isLoading ? (
          <div className="grid place-items-center rounded-3xl border border-purple-100 bg-white py-24 text-indigo-900/50 shadow-md shadow-purple-100/60">
            <Loader2 className="animate-spin" size={24} />
          </div>
        ) : breakdown.data ? (
          <BehaviorBreakdown breakdown={breakdown.data} currencyIcon={currencyIcon} />
        ) : null}
      </div>
    </div>
  );
}
