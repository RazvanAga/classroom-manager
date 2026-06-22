"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Pencil, Plus, Sparkles, Trash2, X } from "lucide-react";
import { useState } from "react";
import {
  addBehavior,
  type Behavior,
  fetchBehaviors,
  removeBehavior,
  updateBehavior,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { ConfirmDialog } from "./ConfirmDialog";
import { SettingsCard } from "./SettingsCard";

// The behavior catalog editor (issue #26): add, edit in place, and remove the per-class behaviors a
// teacher awards. Default points are signed (rewards positive, penalties negative). Reuses the #4
// behavior endpoints; awarding lives on the dashboard.
export function BehaviorsSettings({
  classId,
  currencyIcon,
}: {
  classId: string;
  currencyIcon: CurrencyIcon;
}) {
  const queryClient = useQueryClient();
  const behaviors = useQuery({
    queryKey: ["behaviors", classId],
    queryFn: () => fetchBehaviors(classId),
  });

  const [name, setName] = useState("");
  const [points, setPoints] = useState("1");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [removing, setRemoving] = useState<Behavior | null>(null);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["behaviors", classId] });

  const add = useMutation({
    mutationFn: () => addBehavior(classId, name.trim(), parsePoints(points)),
    onSuccess: async () => {
      setName("");
      setPoints("1");
      await invalidate();
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => removeBehavior(classId, id),
    onSuccess: async () => {
      setRemoving(null);
      await invalidate();
    },
  });

  const list = [...(behaviors.data ?? [])].sort((a, b) => b.defaultPoints - a.defaultPoints);
  const canAdd = name.trim().length > 0 && !add.isPending;

  return (
    <SettingsCard
      icon={Sparkles}
      title={ro.settings.behaviors.title}
      subtitle={ro.settings.behaviors.subtitle}
      badge={behaviors.data ? behaviors.data.length : undefined}
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (canAdd) add.mutate();
        }}
        className="mb-5 flex flex-wrap gap-2"
      >
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder={ro.settings.behaviors.namePlaceholder}
          className="min-w-[10rem] flex-1 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm transition focus:border-violet-300 focus:bg-white focus:outline-none"
        />
        <input
          type="number"
          value={points}
          onChange={(e) => setPoints(e.target.value)}
          placeholder={ro.settings.behaviors.pointsPlaceholder}
          className="w-24 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm tabular-nums transition focus:border-violet-300 focus:bg-white focus:outline-none"
        />
        <button
          type="submit"
          disabled={!canAdd}
          className="flex items-center gap-1.5 rounded-2xl bg-violet-600 px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-50"
        >
          <Plus size={16} /> {add.isPending ? ro.settings.behaviors.adding : ro.settings.behaviors.add}
        </button>
      </form>
      <p className="mb-4 text-xs text-slate-400">{ro.settings.behaviors.pointsHint}</p>

      {add.isError && (
        <p className="mb-3 text-sm font-semibold text-rose-600">{(add.error as Error).message}</p>
      )}

      {list.length === 0 ? (
        <p className="py-6 text-center text-sm text-slate-400">{ro.settings.behaviors.empty}</p>
      ) : (
        <ul className="space-y-2">
          {list.map((b) =>
            editingId === b.id ? (
              <EditRow
                key={b.id}
                classId={classId}
                behavior={b}
                onDone={() => setEditingId(null)}
              />
            ) : (
              <li
                key={b.id}
                className="flex items-center gap-3 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5"
              >
                <span className="flex-1 truncate text-sm font-semibold text-slate-700">{b.name}</span>
                <Points value={b.defaultPoints} icon={currencyIcon} size={13} />
                <button
                  type="button"
                  onClick={() => setEditingId(b.id)}
                  className="text-slate-300 transition hover:text-violet-500"
                  aria-label={ro.settings.behaviors.edit}
                >
                  <Pencil size={16} />
                </button>
                <button
                  type="button"
                  onClick={() => setRemoving(b)}
                  className="text-slate-300 transition hover:text-rose-500"
                  aria-label={ro.settings.behaviors.remove}
                >
                  <Trash2 size={16} />
                </button>
              </li>
            ),
          )}
        </ul>
      )}

      {removing && (
        <ConfirmDialog
          title={ro.settings.behaviors.removeConfirmTitle}
          message={ro.settings.behaviors.removeConfirmMessage(removing.name)}
          confirmLabel={ro.settings.behaviors.removeConfirm}
          pendingLabel={ro.settings.behaviors.removing}
          isPending={remove.isPending}
          error={remove.isError ? (remove.error as Error).message : null}
          onConfirm={() => remove.mutate(removing.id)}
          onClose={() => setRemoving(null)}
        />
      )}
    </SettingsCard>
  );
}

// Inline editor for one behavior — name + signed points, saved via the #4 update endpoint.
function EditRow({
  classId,
  behavior,
  onDone,
}: {
  classId: string;
  behavior: Behavior;
  onDone: () => void;
}) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(behavior.name);
  const [points, setPoints] = useState(String(behavior.defaultPoints));

  const save = useMutation({
    mutationFn: () => updateBehavior(classId, behavior.id, name.trim(), parsePoints(points)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["behaviors", classId] });
      onDone();
    },
  });

  const canSave = name.trim().length > 0 && !save.isPending;

  return (
    <li className="flex flex-wrap items-center gap-2 rounded-2xl border-2 border-violet-200 bg-violet-50 px-3 py-2.5">
      <input
        type="text"
        value={name}
        onChange={(e) => setName(e.target.value)}
        className="min-w-[8rem] flex-1 rounded-xl border-2 border-violet-100 bg-white px-3 py-2 text-sm transition focus:border-violet-300 focus:outline-none"
      />
      <input
        type="number"
        value={points}
        onChange={(e) => setPoints(e.target.value)}
        className="w-20 rounded-xl border-2 border-violet-100 bg-white px-3 py-2 text-sm tabular-nums transition focus:border-violet-300 focus:outline-none"
      />
      <button
        type="button"
        onClick={() => canSave && save.mutate()}
        disabled={!canSave}
        className="flex items-center gap-1 rounded-xl bg-violet-600 px-3 py-2 text-xs font-bold text-white transition hover:bg-violet-700 disabled:opacity-50"
      >
        <Check size={14} /> {save.isPending ? ro.settings.behaviors.saving : ro.settings.behaviors.save}
      </button>
      <button
        type="button"
        onClick={onDone}
        className="flex items-center gap-1 rounded-xl px-2 py-2 text-xs font-bold text-slate-500 transition hover:bg-white"
        aria-label={ro.settings.behaviors.cancel}
      >
        <X size={14} />
      </button>
      {save.isError && (
        <p className="w-full text-sm font-semibold text-rose-600">{(save.error as Error).message}</p>
      )}
    </li>
  );
}

// Parse the points input to a signed integer; an empty or malformed value becomes 0 (the server
// allows any signed int, so zero is harmless and never blocks a save).
function parsePoints(raw: string): number {
  const n = Number.parseInt(raw, 10);
  return Number.isFinite(n) ? n : 0;
}
