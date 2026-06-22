"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ListPlus, Plus, Trash2, UserMinus, Users } from "lucide-react";
import { useState } from "react";
import {
  addStudent,
  bulkAddStudents,
  fetchStudents,
  type Gender,
  purgeStudent,
  removeStudent,
  type Student,
} from "@/lib/api";
import { ro } from "@/lib/strings";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";
import { ConfirmDialog } from "./ConfirmDialog";
import { SettingsCard } from "./SettingsCard";

// The roster editor (issue #26): single add (name + optional gender), bulk paste (one student per
// line, optional `, F`/`, M`), undoable soft-remove, and — for Owners only — an irreversible purge
// behind a typed confirmation. Reuses the #3 roster + #15 purge endpoints. `isOwner` gates purge in
// the UI; the server fails closed for non-owners regardless.
export function RosterSettings({
  classId,
  isOwner,
}: {
  classId: string;
  isOwner: boolean;
}) {
  const queryClient = useQueryClient();
  const students = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });

  const [name, setName] = useState("");
  const [gender, setGender] = useState<Gender | "">("");
  const [bulkOpen, setBulkOpen] = useState(false);
  const [bulkText, setBulkText] = useState("");
  const [removing, setRemoving] = useState<Student | null>(null);
  const [purging, setPurging] = useState<Student | null>(null);

  // Roster changes shift the leaderboard and recent feed too, so refresh those alongside the list.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["students", classId] }),
      queryClient.invalidateQueries({ queryKey: ["leaderboard", classId] }),
      queryClient.invalidateQueries({ queryKey: ["transactions", classId] }),
    ]);

  const add = useMutation({
    mutationFn: () => addStudent(classId, name.trim(), gender || null),
    onSuccess: async () => {
      setName("");
      setGender("");
      await invalidate();
    },
  });

  const bulk = useMutation({
    mutationFn: () => bulkAddStudents(classId, bulkText),
    onSuccess: async () => {
      setBulkText("");
      setBulkOpen(false);
      await invalidate();
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => removeStudent(classId, id),
    onSuccess: async () => {
      setRemoving(null);
      await invalidate();
    },
  });

  const purge = useMutation({
    mutationFn: (id: string) => purgeStudent(classId, id),
    onSuccess: async () => {
      setPurging(null);
      await invalidate();
    },
  });

  const roster = [...(students.data ?? [])].sort((a, b) =>
    a.displayName.localeCompare(b.displayName),
  );
  const canAdd = name.trim().length > 0 && !add.isPending;

  return (
    <SettingsCard
      icon={Users}
      title={ro.settings.roster.title}
      subtitle={ro.settings.roster.subtitle}
      badge={students.data ? students.data.length : undefined}
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (canAdd) add.mutate();
        }}
        className="flex flex-wrap gap-2"
      >
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder={ro.settings.roster.namePlaceholder}
          className="min-w-[10rem] flex-1 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm transition focus:border-violet-300 focus:bg-white focus:outline-none"
        />
        <select
          value={gender}
          onChange={(e) => setGender(e.target.value as Gender | "")}
          className="rounded-2xl border-2 border-slate-100 bg-slate-50 px-3 py-2.5 text-sm font-medium transition focus:border-violet-300 focus:outline-none"
        >
          <option value="">{ro.settings.roster.genderUnset}</option>
          <option value="Female">{ro.settings.roster.genderGirl}</option>
          <option value="Male">{ro.settings.roster.genderBoy}</option>
        </select>
        <button
          type="submit"
          disabled={!canAdd}
          className="flex items-center gap-1.5 rounded-2xl bg-violet-600 px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-50"
        >
          <Plus size={16} /> {add.isPending ? ro.settings.roster.adding : ro.settings.roster.add}
        </button>
      </form>

      {add.isError && (
        <p className="mt-2 text-sm font-semibold text-rose-600">{(add.error as Error).message}</p>
      )}

      <button
        type="button"
        onClick={() => setBulkOpen((v) => !v)}
        className="mt-3 flex items-center gap-1.5 text-sm font-semibold text-violet-600 transition hover:text-violet-800"
      >
        <ListPlus size={16} /> {ro.settings.roster.bulkToggle}
      </button>

      {bulkOpen && (
        <div className="mt-3 space-y-2">
          <textarea
            value={bulkText}
            onChange={(e) => setBulkText(e.target.value)}
            rows={5}
            placeholder={ro.settings.roster.bulkPlaceholder}
            className="w-full resize-y rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-3 text-sm transition focus:border-violet-300 focus:bg-white focus:outline-none"
          />
          <p className="text-xs text-slate-400">{ro.settings.roster.bulkHint}</p>
          {bulk.isError && (
            <p className="text-sm font-semibold text-rose-600">{(bulk.error as Error).message}</p>
          )}
          <button
            type="button"
            onClick={() => bulk.mutate()}
            disabled={bulk.isPending || bulkText.trim().length === 0}
            className="flex items-center gap-1.5 rounded-2xl bg-violet-100 px-5 py-2.5 text-sm font-bold text-violet-700 transition hover:bg-violet-200 disabled:opacity-50"
          >
            <ListPlus size={16} />
            {bulk.isPending ? ro.settings.roster.bulkAdding : ro.settings.roster.bulkAdd}
          </button>
        </div>
      )}

      <div className="mt-5 border-t border-slate-100 pt-5">
        {roster.length === 0 ? (
          <p className="py-6 text-center text-sm text-slate-400">{ro.settings.roster.empty}</p>
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2">
            {roster.map((s) => (
              <li
                key={s.id}
                className="flex items-center gap-2.5 rounded-2xl border-2 border-slate-100 bg-slate-50 px-3 py-2"
              >
                <StudentAvatar classId={classId} studentId={s.id} size={32} className="rounded-lg" />
                <span className="flex-1 truncate text-sm font-semibold text-slate-700">
                  {s.displayName}
                </span>
                {isOwner && (
                  <button
                    type="button"
                    onClick={() => setPurging(s)}
                    className="text-slate-300 transition hover:text-rose-600"
                    aria-label={ro.settings.roster.purge}
                    title={ro.settings.roster.purge}
                  >
                    <Trash2 size={16} />
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setRemoving(s)}
                  className="text-slate-300 transition hover:text-amber-600"
                  aria-label={ro.settings.roster.remove}
                  title={ro.settings.roster.remove}
                >
                  <UserMinus size={16} />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {removing && (
        <ConfirmDialog
          title={ro.settings.roster.removeConfirmTitle}
          message={ro.settings.roster.removeConfirmMessage(removing.displayName)}
          confirmLabel={ro.settings.roster.removeConfirm}
          pendingLabel={ro.settings.roster.removing}
          isPending={remove.isPending}
          error={remove.isError ? (remove.error as Error).message : null}
          onConfirm={() => remove.mutate(removing.id)}
          onClose={() => setRemoving(null)}
        />
      )}

      {purging && (
        <ConfirmDialog
          title={ro.settings.purge.title}
          message={ro.settings.purge.message(purging.displayName)}
          confirmLabel={ro.settings.purge.confirm}
          pendingLabel={ro.settings.purge.purging}
          confirmWord={ro.settings.purge.confirmWord}
          confirmWordHint={ro.settings.purge.typeHint(ro.settings.purge.confirmWord)}
          isPending={purge.isPending}
          error={purge.isError ? (purge.error as Error).message : null}
          onConfirm={() => purge.mutate(purging.id)}
          onClose={() => setPurging(null)}
        />
      )}
    </SettingsCard>
  );
}
