"use client";

import { useQuery } from "@tanstack/react-query";
import { Loader2, Users } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useMemo } from "react";
import { fetchBehaviors, fetchClasses, fetchStudents } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { GroupMaker } from "@/components/groups/GroupMaker";
import { StudentPicker } from "@/components/groups/StudentPicker";
import { Timer } from "@/components/groups/Timer";

// Grupuri (issue #24): the class-tools page — random student picker (server fair picker), group maker
// (deterministic GroupFormer + bulk award + saved groupings), and a client-only activity timer, stacked
// on one page. The picker and group maker need the roster; the timer is self-contained. Class-scoped
// via ?class=.
export default function GrupuriPage() {
  const classId = useSearchParams().get("class") ?? "";

  const classes = useQuery({ queryKey: ["classes"], queryFn: fetchClasses });
  const students = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
    enabled: !!classId,
  });
  const behaviors = useQuery({
    queryKey: ["behaviors", classId],
    queryFn: () => fetchBehaviors(classId),
    enabled: !!classId,
  });

  const currencyIcon: CurrencyIcon =
    classes.data?.find((c) => c.id === classId)?.currencyIcon ?? "Star";

  const roster = useMemo(
    () => [...(students.data ?? [])].sort((a, b) => a.displayName.localeCompare(b.displayName)),
    [students.data],
  );

  if (students.isLoading || behaviors.isLoading) {
    return (
      <div className="grid place-items-center py-24 text-indigo-900/60">
        <Loader2 className="animate-spin" size={28} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {roster.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-16 text-center">
          <Users size={40} className="mx-auto text-violet-300" />
          <p className="mt-3 text-lg font-bold text-slate-600">{ro.groups.emptyRoster}</p>
          <p className="mt-1 text-sm text-slate-400">
            <Link href={`/setari?class=${classId}`} className="font-semibold text-violet-600 underline">
              {ro.dashboard.emptyRosterHint}
            </Link>
          </p>
        </div>
      ) : (
        <>
          <StudentPicker classId={classId} students={roster} />
          <GroupMaker
            classId={classId}
            students={roster}
            behaviors={behaviors.data ?? []}
            currencyIcon={currencyIcon}
          />
        </>
      )}

      {/* The timer is class-independent and useful even with an empty roster, so it always renders. */}
      <Timer />
    </div>
  );
}
