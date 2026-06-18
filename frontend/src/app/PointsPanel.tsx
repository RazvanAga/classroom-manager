"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  awardBehavior,
  fetchBehaviors,
  fetchLeaderboard,
  fetchStudents,
  fetchTransactions,
  voidBatch,
  voidTransaction,
} from "@/lib/api";

// Award points for the selected class: pick students, tap a behavior, and watch the leaderboard
// (ranked by lifetime earned) update. Wallet is shown alongside but never reorders the ranking.
export function PointsPanel({ classId }: { classId: string }) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { data: students } = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });
  const { data: behaviors } = useQuery({
    queryKey: ["behaviors", classId],
    queryFn: () => fetchBehaviors(classId),
  });
  const { data: leaderboard, isLoading: loadingBoard } = useQuery({
    queryKey: ["leaderboard", classId],
    queryFn: () => fetchLeaderboard(classId),
  });
  const { data: activity } = useQuery({
    queryKey: ["transactions", classId],
    queryFn: () => fetchTransactions(classId),
  });

  // Both the leaderboard and the activity feed shift whenever points are awarded or undone.
  const refreshTotals = () => {
    queryClient.invalidateQueries({ queryKey: ["leaderboard", classId] });
    queryClient.invalidateQueries({ queryKey: ["transactions", classId] });
  };

  const award = useMutation({
    mutationFn: (behaviorId: string) =>
      awardBehavior(classId, [...selected], behaviorId),
    onSuccess: () => {
      // Clear the selection so the next behavior isn't accidentally awarded to the previous group.
      setSelected(new Set());
      refreshTotals();
    },
  });

  const undo = useMutation({
    mutationFn: (tx: { id: string; batchId: string | null }) =>
      tx.batchId ? voidBatch(classId, tx.batchId) : voidTransaction(classId, tx.id),
    onSuccess: refreshTotals,
  });

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const allSelected = !!students && students.length > 0 && selected.size === students.length;
  const toggleAll = () =>
    setSelected(allSelected ? new Set() : new Set((students ?? []).map((s) => s.id)));

  const canAward = selected.size > 0 && !award.isPending;

  return (
    <section className="roster">
      <h2>Points</h2>

      {students && students.length > 0 ? (
        <>
          <div className="points-toolbar">
            <button className="btn-ghost" onClick={toggleAll}>
              {allSelected ? "Clear all" : "Select all"}
            </button>
            <span className="empty selected-count">
              {selected.size} selected
            </span>
          </div>

          <ul className="class-list">
            {students.map((s) => (
              <li
                key={s.id}
                className={`class-item selectable${selected.has(s.id) ? " selected" : ""}`}
                onClick={() => toggle(s.id)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    toggle(s.id);
                  }
                }}
              >
                <span className="class-name">{s.displayName}</span>
                <input
                  type="checkbox"
                  className="pick-box"
                  checked={selected.has(s.id)}
                  readOnly
                  tabIndex={-1}
                  aria-label={`Select ${s.displayName}`}
                />
              </li>
            ))}
          </ul>

          <p className="award-hint">
            {canAward
              ? "Tap a behavior to award the selected students:"
              : "Select students, then tap a behavior to award."}
          </p>
          <div className="behavior-chips">
            {(behaviors ?? []).map((b) => (
              <button
                key={b.id}
                className={`chip${b.defaultPoints < 0 ? " negative" : ""}`}
                disabled={!canAward}
                onClick={() => award.mutate(b.id)}
              >
                {b.name}{" "}
                <span className="chip-points">
                  {b.defaultPoints > 0 ? `+${b.defaultPoints}` : b.defaultPoints}
                </span>
              </button>
            ))}
          </div>
        </>
      ) : (
        <p className="empty">Add students to start awarding points.</p>
      )}

      {award.isError && <p className="error">{(award.error as Error).message}</p>}

      <h3 className="leaderboard-title">Leaderboard</h3>
      {loadingBoard ? (
        <p className="empty">Loading leaderboard…</p>
      ) : leaderboard && leaderboard.length > 0 ? (
        <ol className="class-list leaderboard">
          {leaderboard.map((e, i) => (
            <li key={e.studentId} className="class-item">
              <span className="class-name">
                <span className="rank">{i + 1}.</span> {e.displayName}
              </span>
              <span className="roster-right">
                <span className="points-tag">{e.lifetimeEarned} lifetime</span>
                <span className={`points-tag${e.wallet < 0 ? " negative" : " wallet"}`}>
                  {e.wallet} wallet
                </span>
              </span>
            </li>
          ))}
        </ol>
      ) : (
        <p className="empty">No points awarded yet.</p>
      )}

      {undo.isError && <p className="error">{(undo.error as Error).message}</p>}

      {activity && activity.length > 0 && (
        <>
          <h3 className="leaderboard-title">Recent activity</h3>
          <ul className="class-list">
            {activity.map((t) => {
              const voided = t.voidedAt !== null;
              const label = t.behaviorName ?? t.reason ?? "Adjustment";
              return (
                <li key={t.id} className={`class-item${voided ? " voided" : ""}`}>
                  <span className="class-name">
                    {t.studentName}{" "}
                    <span className="activity-label">{label}</span>
                  </span>
                  <span className="roster-right">
                    <span className={`points-tag${t.amount < 0 ? " negative" : ""}`}>
                      {t.amount > 0 ? `+${t.amount}` : t.amount}
                    </span>
                    {t.batchId && <span className="role-tag">bulk</span>}
                    {voided ? (
                      <span className="role-tag">voided</span>
                    ) : (
                      <button
                        className="btn-ghost danger"
                        disabled={undo.isPending}
                        onClick={() => undo.mutate({ id: t.id, batchId: t.batchId })}
                      >
                        {t.batchId ? "Undo batch" : "Undo"}
                      </button>
                    )}
                  </span>
                </li>
              );
            })}
          </ul>
        </>
      )}
    </section>
  );
}
