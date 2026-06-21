"use client";

import { useQuery } from "@tanstack/react-query";
import { Loader2, Users } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useMemo, useState } from "react";
import { fetchClasses, fetchLeaderboard, fetchStudents } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";
import { StudentShop } from "@/components/shop/StudentShop";

// Magazin (issue #23): pick a student to open their avatar store. The shop itself lives in StudentShop;
// this page owns the roster selection and threads the class currency icon through.
export default function MagazinPage() {
  const classId = useSearchParams().get("class") ?? "";
  const [selectedId, setSelectedId] = useState<string | null>(null);

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

  const currencyIcon: CurrencyIcon =
    classes.data?.find((c) => c.id === classId)?.currencyIcon ?? "Star";

  const walletByStudent = useMemo(() => {
    const map = new Map<string, number>();
    for (const e of leaderboard.data ?? []) map.set(e.studentId, e.wallet);
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
    return (
      <StudentShop
        classId={classId}
        studentId={selected.id}
        studentName={selected.displayName}
        currencyIcon={currencyIcon}
        onBack={() => setSelectedId(null)}
      />
    );
  }

  if (roster.length === 0) {
    return (
      <div className="rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-16 text-center">
        <Users size={40} className="mx-auto text-violet-300" />
        <p className="mt-3 text-lg font-bold text-slate-600">{ro.shop.emptyRoster}</p>
        <p className="mt-1 text-sm text-slate-400">
          <Link href={`/setari?class=${classId}`} className="font-semibold text-violet-600 underline">
            {ro.dashboard.emptyRosterHint}
          </Link>
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm font-semibold text-slate-500">{ro.shop.pickStudent}</p>
      <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 lg:grid-cols-6">
        {roster.map((s) => (
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
              value={walletByStudent.get(s.id) ?? 0}
              icon={currencyIcon}
              size={11}
              tone="neutral"
              showSign={false}
            />
          </button>
        ))}
      </div>
    </div>
  );
}
