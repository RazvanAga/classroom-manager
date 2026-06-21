"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Award, FolderOpen, Save, Shuffle, Users } from "lucide-react";
import { useState } from "react";
import {
  type Behavior,
  type FormedGroup,
  fetchGrouping,
  fetchGroupings,
  previewGrouping,
  saveGrouping,
  type Student,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { AwardModal, type AwardTarget } from "@/components/dashboard/AwardModal";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";

// Group maker (issue #24). Forms groups by group size with optional gender balancing — both run through
// the server's deterministic GroupFormer, so a preview carries a `seed` that `save` replays to reproduce
// the exact arrangement. A whole group can be awarded a behavior in one bulk batch (via AwardModal), and
// saved groupings reopen identically. The displayed arrangement is the server's, never re-shuffled here.

const GROUP_COLORS = [
  "border-violet-300 bg-violet-50 text-violet-700",
  "border-emerald-300 bg-emerald-50 text-emerald-700",
  "border-amber-300 bg-amber-50 text-amber-700",
  "border-rose-300 bg-rose-50 text-rose-700",
  "border-sky-300 bg-sky-50 text-sky-700",
  "border-pink-300 bg-pink-50 text-pink-700",
];

// What `save` needs to reproduce the current preview; null once it's a reopened (already-saved) grouping.
type Savable = { seed: number; groupSize: number; balanceByGender: boolean };

export function GroupMaker({
  classId,
  students,
  behaviors,
  currencyIcon,
}: {
  classId: string;
  students: Student[];
  behaviors: Behavior[];
  currencyIcon: CurrencyIcon;
}) {
  const queryClient = useQueryClient();
  const [groupSize, setGroupSize] = useState(3);
  const [balanceByGender, setBalanceByGender] = useState(false);
  const [groups, setGroups] = useState<FormedGroup[] | null>(null);
  const [savable, setSavable] = useState<Savable | null>(null);
  const [name, setName] = useState("");
  const [target, setTarget] = useState<AwardTarget | null>(null);

  const maxSize = Math.max(2, students.length);

  const groupings = useQuery({
    queryKey: ["groupings", classId],
    queryFn: () => fetchGroupings(classId),
  });

  const form = useMutation({
    mutationFn: () => previewGrouping(classId, groupSize, balanceByGender),
    onSuccess: (res) => {
      setGroups(res.groups);
      setSavable({ seed: res.seed, groupSize: res.groupSize, balanceByGender: res.balancedByGender });
      setName("");
    },
  });

  const persist = useMutation({
    mutationFn: () =>
      saveGrouping(classId, name.trim() || null, savable!.groupSize, savable!.balanceByGender, savable!.seed),
    onSuccess: async () => {
      setSavable(null);
      setName("");
      await queryClient.invalidateQueries({ queryKey: ["groupings", classId] });
    },
  });

  const reopen = useMutation({
    mutationFn: (groupingId: string) => fetchGrouping(classId, groupingId),
    onSuccess: (res) => {
      setGroups(res.groups);
      setSavable(null); // already persisted — nothing to re-save
    },
  });

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-6 shadow-sm">
      <h2 className="mb-5 flex items-center gap-2 text-lg font-extrabold text-slate-800">
        <Users size={20} className="text-violet-500" /> {ro.groups.maker.title}
      </h2>

      <div className="mb-6 flex flex-wrap items-center gap-4">
        <div className="flex items-center gap-3">
          <span className="text-sm font-semibold text-slate-600">{ro.groups.maker.groupSize}:</span>
          <div className="flex items-center gap-3 rounded-2xl bg-violet-50 p-1">
            <button
              type="button"
              onClick={() => setGroupSize((n) => Math.max(2, n - 1))}
              className="h-9 w-9 rounded-xl border border-violet-200 bg-white text-lg font-extrabold text-violet-700 shadow-sm transition hover:bg-violet-100"
            >
              −
            </button>
            <span className="w-8 text-center text-lg font-extrabold text-violet-800">{groupSize}</span>
            <button
              type="button"
              onClick={() => setGroupSize((n) => Math.min(maxSize, n + 1))}
              className="h-9 w-9 rounded-xl border border-violet-200 bg-white text-lg font-extrabold text-violet-700 shadow-sm transition hover:bg-violet-100"
            >
              +
            </button>
          </div>
        </div>

        <label className="flex cursor-pointer items-center gap-2.5 rounded-2xl border border-violet-100 bg-violet-50 px-4 py-2.5">
          <input
            type="checkbox"
            checked={balanceByGender}
            onChange={(e) => setBalanceByGender(e.target.checked)}
            className="h-4 w-4 cursor-pointer accent-violet-600"
          />
          <span className="text-sm font-medium text-slate-700">{ro.groups.maker.balanceGender}</span>
        </label>

        <button
          type="button"
          onClick={() => form.mutate()}
          disabled={form.isPending}
          className="flex items-center gap-2 rounded-2xl bg-gradient-to-r from-violet-600 to-indigo-500 px-6 py-2.5 font-extrabold text-white shadow shadow-purple-200 transition hover:opacity-90 active:scale-95 disabled:opacity-50"
        >
          <Shuffle size={17} /> {form.isPending ? ro.groups.maker.forming : ro.groups.maker.form}
        </button>
      </div>

      {form.isError && (
        <p className="mb-4 text-sm font-semibold text-rose-600">{(form.error as Error).message}</p>
      )}

      {groups && (
        <>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
            {groups.map((g, i) => {
              const color = GROUP_COLORS[i % GROUP_COLORS.length];
              return (
                <div key={g.groupNumber} className={`rounded-2xl border-2 p-4 ${color}`}>
                  <p className="mb-3 flex items-center justify-between text-sm font-extrabold">
                    {ro.groups.maker.group(g.groupNumber)}
                    <span className="text-xs font-semibold opacity-70">
                      {ro.groups.maker.members(g.members.length)}
                    </span>
                  </p>
                  <ul className="mb-4 space-y-2">
                    {g.members.map((m) => (
                      <li key={m.studentId} className="flex items-center gap-2">
                        <StudentAvatar classId={classId} studentId={m.studentId} size={26} className="rounded-lg" />
                        <span className="truncate text-sm font-medium leading-tight text-slate-700">
                          {m.displayName}
                        </span>
                      </li>
                    ))}
                  </ul>
                  <button
                    type="button"
                    disabled={behaviors.length === 0}
                    onClick={() =>
                      setTarget({
                        studentIds: g.members.map((m) => m.studentId),
                        title: ro.groups.maker.group(g.groupNumber),
                      })
                    }
                    className="flex w-full items-center justify-center gap-1.5 rounded-xl bg-white/70 py-1.5 text-xs font-bold text-slate-700 transition hover:bg-white disabled:opacity-50"
                  >
                    <Award size={14} /> {ro.groups.maker.award}
                  </button>
                </div>
              );
            })}
          </div>

          {/* Save the just-previewed arrangement (hidden once it's a reopened, already-saved one). */}
          {savable && (
            <div className="mt-5 flex flex-wrap items-center gap-2">
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={ro.groups.maker.namePlaceholder}
                className="flex-1 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm transition focus:border-violet-300 focus:bg-white focus:outline-none"
              />
              <button
                type="button"
                onClick={() => persist.mutate()}
                disabled={persist.isPending}
                className="flex items-center gap-2 rounded-2xl bg-violet-100 px-5 py-2.5 text-sm font-bold text-violet-700 transition hover:bg-violet-200 disabled:opacity-50"
              >
                <Save size={16} /> {persist.isPending ? ro.groups.maker.saving : ro.groups.maker.save}
              </button>
            </div>
          )}
          {persist.isError && (
            <p className="mt-2 text-sm font-semibold text-rose-600">{(persist.error as Error).message}</p>
          )}
        </>
      )}

      {/* Saved groupings — reopen reproduces the exact arrangement from its seed. */}
      {(groupings.data?.length ?? 0) > 0 && (
        <div className="mt-6 border-t border-slate-100 pt-5">
          <p className="mb-3 flex items-center gap-1.5 text-xs font-extrabold uppercase tracking-wide text-slate-500">
            <FolderOpen size={12} /> {ro.groups.maker.saved}
          </p>
          <div className="flex flex-wrap gap-2">
            {groupings.data!.map((s) => (
              <button
                key={s.id}
                type="button"
                onClick={() => reopen.mutate(s.id)}
                disabled={reopen.isPending}
                className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:border-violet-300 hover:bg-violet-50 disabled:opacity-50"
              >
                <FolderOpen size={14} className="text-violet-500" />
                {s.name || ro.groups.maker.unnamed}
                <span className="text-xs font-normal text-slate-400">
                  · {ro.groups.maker.members(s.studentCount)}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}

      {target && (
        <AwardModal
          classId={classId}
          target={target}
          behaviors={behaviors}
          currencyIcon={currencyIcon}
          onClose={() => setTarget(null)}
        />
      )}
    </section>
  );
}
