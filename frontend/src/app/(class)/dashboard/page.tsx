"use client";

import { useQuery } from "@tanstack/react-query";
import { CheckSquare, Loader2, Users, X } from "lucide-react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { useMemo, useState } from "react";
import {
  fetchBehaviors,
  fetchClasses,
  fetchLeaderboard,
  fetchStudents,
  fetchTransactions,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { AwardModal, type AwardTarget } from "@/components/dashboard/AwardModal";
import { Leaderboard } from "@/components/dashboard/Leaderboard";
import { RecentActivity } from "@/components/dashboard/RecentActivity";
import { StudentCard } from "@/components/dashboard/StudentCard";

// The class dashboard (issue #22): the screen a teacher lives in all day — a student grid with quick
// awarding, an all-time leaderboard, and an undoable recent-activity feed. Class-scoped via ?class=.
export default function DashboardPage() {
  const classId = useSearchParams().get("class") ?? "";

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
  const behaviors = useQuery({
    queryKey: ["behaviors", classId],
    queryFn: () => fetchBehaviors(classId),
    enabled: !!classId,
  });
  const transactions = useQuery({
    queryKey: ["transactions", classId],
    queryFn: () => fetchTransactions(classId),
    enabled: !!classId,
  });

  const currencyIcon: CurrencyIcon =
    classes.data?.find((c) => c.id === classId)?.currencyIcon ?? "Star";

  const [multiSelect, setMultiSelect] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [target, setTarget] = useState<AwardTarget | null>(null);

  // Wallet + grid rank from the leaderboard rows (the only source needed for points). Grid rank goes by
  // spendable wallet — the value shown on the card — while the leaderboard panel ranks by lifetime.
  const { walletByStudent, rankByStudent } = useMemo(() => {
    const wallet = new Map<string, number>();
    const rank = new Map<string, number>();
    const rows = [...(leaderboard.data ?? [])];
    for (const r of rows) wallet.set(r.studentId, r.wallet);
    rows
      .sort((a, b) => b.wallet - a.wallet || a.displayName.localeCompare(b.displayName))
      .forEach((r, i) => rank.set(r.studentId, i + 1));
    return { walletByStudent: wallet, rankByStudent: rank };
  }, [leaderboard.data]);

  const roster = useMemo(
    () => [...(students.data ?? [])].sort((a, b) => a.displayName.localeCompare(b.displayName)),
    [students.data],
  );

  function toggleSelected(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function exitMultiSelect() {
    setMultiSelect(false);
    setSelectedIds(new Set());
  }

  function onCardClick(id: string, displayName: string) {
    if (multiSelect) {
      toggleSelected(id);
      return;
    }
    setTarget({
      studentIds: [id],
      studentId: id,
      title: displayName,
      wallet: walletByStudent.get(id) ?? 0,
    });
  }

  const loading =
    students.isLoading || leaderboard.isLoading || behaviors.isLoading || transactions.isLoading;

  if (loading) {
    return (
      <div className="grid place-items-center py-24 text-indigo-900/60">
        <Loader2 className="animate-spin" size={28} />
      </div>
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <div className="space-y-4 lg:col-span-2">
        <Toolbar
          multiSelect={multiSelect}
          selectedCount={selectedIds.size}
          hasStudents={roster.length > 0}
          onToggleMulti={() => (multiSelect ? exitMultiSelect() : setMultiSelect(true))}
          onApplySelected={() =>
            setTarget({
              studentIds: [...selectedIds],
              title: ro.dashboard.selected(selectedIds.size),
            })
          }
          onWholeClass={() =>
            setTarget({
              studentIds: roster.map((s) => s.id),
              title: ro.dashboard.wholeClass,
            })
          }
        />

        {roster.length === 0 ? (
          <EmptyRoster classId={classId} />
        ) : (
          <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 lg:grid-cols-5">
            {roster.map((s) => (
              <StudentCard
                key={s.id}
                classId={classId}
                student={s}
                wallet={walletByStudent.get(s.id) ?? 0}
                rank={rankByStudent.get(s.id) ?? roster.length}
                currencyIcon={currencyIcon}
                selectable={multiSelect}
                selected={selectedIds.has(s.id)}
                onClick={() => onCardClick(s.id, s.displayName)}
              />
            ))}
          </div>
        )}
      </div>

      <div className="space-y-6">
        <Leaderboard entries={leaderboard.data ?? []} currencyIcon={currencyIcon} />
        <RecentActivity
          classId={classId}
          transactions={transactions.data ?? []}
          currencyIcon={currencyIcon}
        />
      </div>

      {target && (
        <AwardModal
          classId={classId}
          target={target}
          behaviors={behaviors.data ?? []}
          currencyIcon={currencyIcon}
          onClose={() => setTarget(null)}
          onApplied={exitMultiSelect}
        />
      )}
    </div>
  );
}

function Toolbar({
  multiSelect,
  selectedCount,
  hasStudents,
  onToggleMulti,
  onApplySelected,
  onWholeClass,
}: {
  multiSelect: boolean;
  selectedCount: number;
  hasStudents: boolean;
  onToggleMulti: () => void;
  onApplySelected: () => void;
  onWholeClass: () => void;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={onWholeClass}
        disabled={!hasStudents}
        className="flex items-center gap-2 rounded-2xl bg-violet-100 px-4 py-2 text-sm font-bold text-violet-700 transition hover:bg-violet-200 disabled:opacity-40"
      >
        <Users size={16} /> {ro.dashboard.wholeClass}
      </button>

      <button
        type="button"
        onClick={onToggleMulti}
        disabled={!hasStudents}
        className={`flex items-center gap-2 rounded-2xl px-4 py-2 text-sm font-bold transition disabled:opacity-40 ${
          multiSelect
            ? "bg-violet-600 text-white hover:bg-violet-700"
            : "bg-violet-100 text-violet-700 hover:bg-violet-200"
        }`}
      >
        {multiSelect ? <X size={16} /> : <CheckSquare size={16} />}
        {multiSelect ? ro.dashboard.multiSelectDone : ro.dashboard.multiSelect}
      </button>

      {multiSelect && (
        <>
          <span className="text-sm font-semibold text-slate-500">
            {ro.dashboard.selected(selectedCount)}
          </span>
          <button
            type="button"
            onClick={onApplySelected}
            disabled={selectedCount === 0}
            className="ml-auto rounded-2xl bg-gradient-to-r from-violet-600 to-indigo-500 px-4 py-2 text-sm font-bold text-white shadow transition hover:opacity-90 disabled:opacity-40"
          >
            {ro.dashboard.applyToSelected}
          </button>
        </>
      )}
    </div>
  );
}

function EmptyRoster({ classId }: { classId: string }) {
  return (
    <div className="rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-16 text-center">
      <Users size={40} className="mx-auto text-violet-300" />
      <p className="mt-3 text-lg font-bold text-slate-600">{ro.dashboard.emptyRoster}</p>
      <p className="mt-1 text-sm text-slate-400">
        <Link href={`/setari?class=${classId}`} className="font-semibold text-violet-600 underline">
          {ro.dashboard.emptyRosterHint}
        </Link>
      </p>
    </div>
  );
}
