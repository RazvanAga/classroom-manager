"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Users, X } from "lucide-react";
import { useState } from "react";
import { awardBehavior, type Behavior } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "./Points";
import { StudentAvatar } from "./StudentAvatar";

// The quick-action sheet: pick a behavior chip (green positive / red negative) plus an optional note,
// applied to the modal's target — one student, a multi-selected subset, or the whole class. Awarding
// goes through the catalog-behavior path so it counts toward lifetime/leaderboard (issue #22).
export type AwardTarget = {
  studentIds: string[];
  title: string;
  // A single-student target carries its id (for the avatar) and wallet; group targets leave these out.
  studentId?: string;
  wallet?: number;
};

export function AwardModal({
  classId,
  target,
  behaviors,
  currencyIcon,
  onClose,
  onApplied,
}: {
  classId: string;
  target: AwardTarget;
  behaviors: Behavior[];
  currencyIcon: CurrencyIcon;
  onClose: () => void;
  onApplied?: () => void;
}) {
  const queryClient = useQueryClient();
  const [note, setNote] = useState("");

  const award = useMutation({
    mutationFn: (behaviorId: string) =>
      awardBehavior(classId, target.studentIds, behaviorId, note),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["leaderboard", classId] }),
        queryClient.invalidateQueries({ queryKey: ["transactions", classId] }),
      ]);
      onApplied?.();
      onClose();
    },
  });

  const positive = behaviors.filter((b) => b.defaultPoints >= 0);
  const negative = behaviors.filter((b) => b.defaultPoints < 0);
  const isGroup = !target.studentId;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-indigo-950/50 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md overflow-hidden rounded-3xl bg-white shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-4 bg-gradient-to-r from-violet-600 to-indigo-500 px-5 py-4">
          <div className="flex h-16 w-16 flex-shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-white/15">
            {target.studentId ? (
              <StudentAvatar classId={classId} studentId={target.studentId} size={64} />
            ) : (
              <Users size={32} className="text-white" />
            )}
          </div>
          <div className="min-w-0 flex-1">
            <h2 className="truncate text-lg font-extrabold leading-tight text-white">
              {target.title}
            </h2>
            <p className="mt-0.5 flex items-center gap-1 text-sm text-purple-100">
              {isGroup ? (
                ro.award.applyingTo(target.studentIds.length)
              ) : (
                <>
                  <Points value={target.wallet ?? 0} icon={currencyIcon} size={13} tone="neutral" showSign={false} />
                  <span className="text-purple-100">{ro.award.available}</span>
                </>
              )}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-white/70 transition hover:text-white"
            aria-label={ro.common.cancel}
          >
            <X size={22} />
          </button>
        </div>

        <div className="space-y-4 p-5">
          <input
            type="text"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder={ro.award.notePlaceholder}
            className="w-full rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm transition focus:border-violet-300 focus:bg-white focus:outline-none"
          />

          {behaviors.length === 0 ? (
            <p className="py-6 text-center text-sm text-slate-400">{ro.award.noBehaviors}</p>
          ) : (
            <div className="space-y-4">
              {positive.length > 0 && (
                <BehaviorChips
                  label={ro.award.givePositive}
                  behaviors={positive}
                  currencyIcon={currencyIcon}
                  disabled={award.isPending}
                  onPick={(id) => award.mutate(id)}
                  tone="positive"
                />
              )}
              {negative.length > 0 && (
                <BehaviorChips
                  label={ro.award.giveNegative}
                  behaviors={negative}
                  currencyIcon={currencyIcon}
                  disabled={award.isPending}
                  onPick={(id) => award.mutate(id)}
                  tone="negative"
                />
              )}
            </div>
          )}

          {award.isError && (
            <p className="text-center text-sm font-semibold text-rose-600">
              {(award.error as Error).message}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

function BehaviorChips({
  label,
  behaviors,
  currencyIcon,
  disabled,
  onPick,
  tone,
}: {
  label: string;
  behaviors: Behavior[];
  currencyIcon: CurrencyIcon;
  disabled: boolean;
  onPick: (behaviorId: string) => void;
  tone: "positive" | "negative";
}) {
  const styles =
    tone === "positive"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100"
      : "border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100";

  return (
    <div>
      <p
        className={`mb-2 text-sm font-bold ${
          tone === "positive" ? "text-emerald-700" : "text-rose-700"
        }`}
      >
        {label}
      </p>
      <div className="flex flex-wrap gap-2">
        {behaviors.map((b) => (
          <button
            key={b.id}
            type="button"
            disabled={disabled}
            onClick={() => onPick(b.id)}
            className={`flex items-center gap-2 rounded-2xl border-2 px-3 py-2 text-sm font-bold transition active:scale-95 disabled:opacity-50 ${styles}`}
          >
            <span>{b.name}</span>
            <Points value={b.defaultPoints} icon={currencyIcon} size={12} />
          </button>
        ))}
      </div>
    </div>
  );
}
