"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { fetchStudents, pickStudent, type PickResult } from "@/lib/api";

// Fair random picker for the selected class. The already-picked set for the current cycle lives in
// component state (per session, no DB table — design.md §6.2); "New round" clears it. The server
// holds the fairness rule and returns the carried set after each pick.
export function PickerPanel({ classId }: { classId: string }) {
  const [pickedIds, setPickedIds] = useState<string[]>([]);
  const [last, setLast] = useState<PickResult | null>(null);

  const { data: students } = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });

  const total = students?.length ?? 0;
  // After a reset the carried set holds just the fresh pick, so "picked this round" stays accurate.
  const pickedThisRound = pickedIds.length;

  const pick = useMutation({
    mutationFn: () => pickStudent(classId, pickedIds),
    onSuccess: (result) => {
      setLast(result);
      setPickedIds(result.alreadyPickedIds);
    },
  });

  const reset = () => {
    setPickedIds([]);
    setLast(null);
    pick.reset();
  };

  return (
    <section className="roster">
      <h2>Random picker</h2>

      <div className="picker-stage">
        {last ? (
          <p className="picker-name">{last.pickedDisplayName}</p>
        ) : (
          <p className="picker-placeholder">Tap “Pick a student” to choose someone fairly.</p>
        )}
        {last?.cycleReset && (
          <p className="picker-reset-note">Everyone’s had a turn — starting a new round.</p>
        )}
      </div>

      <div className="picker-toolbar">
        <button
          className="btn-primary inline"
          onClick={() => pick.mutate()}
          disabled={pick.isPending || total === 0}
        >
          {pick.isPending ? "Picking…" : "Pick a student"}
        </button>
        <button
          className="btn-ghost"
          onClick={reset}
          disabled={pick.isPending || (pickedThisRound === 0 && !last)}
        >
          New round
        </button>
        {total > 0 && (
          <span className="picker-progress">
            {pickedThisRound} of {total} picked this round
          </span>
        )}
      </div>

      {total === 0 && <p className="empty">Add students to the roster to pick from them.</p>}
      {pick.isError && <p className="error">{(pick.error as Error).message}</p>}
    </section>
  );
}
