"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  addBehavior,
  fetchBehaviors,
  removeBehavior,
  updateBehavior,
  type Behavior,
} from "@/lib/api";

// Per-class behavior catalog for the selected class: add, inline-edit, remove.
export function BehaviorPanel({ classId }: { classId: string }) {
  const queryClient = useQueryClient();
  const key = ["behaviors", classId];

  const [name, setName] = useState("");
  const [points, setPoints] = useState("1");

  const { data: behaviors, isLoading } = useQuery({
    queryKey: key,
    queryFn: () => fetchBehaviors(classId),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: key });

  const addMutation = useMutation({
    mutationFn: () => addBehavior(classId, name.trim(), Number(points) || 0),
    onSuccess: () => {
      setName("");
      setPoints("1");
      invalidate();
    },
  });

  const removeMutation = useMutation({
    mutationFn: (id: string) => removeBehavior(classId, id),
    onSuccess: invalidate,
  });

  const error =
    (addMutation.error as Error | null) ?? (removeMutation.error as Error | null);

  return (
    <section className="roster">
      <h2>Behaviors</h2>

      <form
        className="create-row"
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) addMutation.mutate();
        }}
      >
        <input
          aria-label="Behavior name"
          placeholder="Behavior name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <input
          aria-label="Default points"
          className="points-input"
          type="number"
          value={points}
          onChange={(e) => setPoints(e.target.value)}
        />
        <button
          className="btn-primary inline"
          type="submit"
          disabled={addMutation.isPending || !name.trim()}
        >
          {addMutation.isPending ? "Adding…" : "Add"}
        </button>
      </form>

      {error && <p className="error">{error.message}</p>}

      {isLoading ? (
        <p className="empty">Loading behaviors…</p>
      ) : behaviors && behaviors.length > 0 ? (
        <ul className="class-list">
          {behaviors.map((b) => (
            <BehaviorRow
              key={b.id}
              classId={classId}
              behavior={b}
              onChanged={invalidate}
              onRemove={() => removeMutation.mutate(b.id)}
              removing={removeMutation.isPending}
            />
          ))}
        </ul>
      ) : (
        <p className="empty">No behaviors — add one above.</p>
      )}
    </section>
  );
}

function BehaviorRow({
  classId,
  behavior,
  onChanged,
  onRemove,
  removing,
}: {
  classId: string;
  behavior: Behavior;
  onChanged: () => void;
  onRemove: () => void;
  removing: boolean;
}) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(behavior.name);
  const [points, setPoints] = useState(String(behavior.defaultPoints));

  const save = useMutation({
    mutationFn: () => updateBehavior(classId, behavior.id, name.trim(), Number(points) || 0),
    onSuccess: () => {
      setEditing(false);
      onChanged();
    },
  });

  if (editing) {
    return (
      <li className="class-item">
        <form
          className="create-row edit-row"
          onSubmit={(e) => {
            e.preventDefault();
            if (name.trim()) save.mutate();
          }}
        >
          <input
            aria-label="Behavior name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <input
            aria-label="Default points"
            className="points-input"
            type="number"
            value={points}
            onChange={(e) => setPoints(e.target.value)}
          />
          <button className="btn-primary inline" type="submit" disabled={save.isPending}>
            {save.isPending ? "Saving…" : "Save"}
          </button>
          <button className="btn-ghost" type="button" onClick={() => setEditing(false)}>
            Cancel
          </button>
        </form>
      </li>
    );
  }

  return (
    <li className="class-item">
      <span className="class-name">{behavior.name}</span>
      <span className="roster-right">
        <span className={`points-tag${behavior.defaultPoints < 0 ? " negative" : ""}`}>
          {behavior.defaultPoints > 0 ? `+${behavior.defaultPoints}` : behavior.defaultPoints}
        </span>
        <button className="btn-ghost" onClick={() => setEditing(true)}>
          Edit
        </button>
        <button className="btn-ghost danger" onClick={onRemove} disabled={removing}>
          Remove
        </button>
      </span>
    </li>
  );
}
