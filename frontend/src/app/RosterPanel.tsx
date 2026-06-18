"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  addStudent,
  bulkAddStudents,
  fetchStudents,
  purgeStudent,
  removeStudent,
  type ClassSummary,
  type Gender,
} from "@/lib/api";
import { RosterStudent } from "./RosterStudent";

// Inline roster for the selected class. Static export (no Node) rules out a dynamic
// /classes/[id] route without build-time params, so selection stays client-side here.
export function RosterPanel({ klass }: { klass: ClassSummary }) {
  const queryClient = useQueryClient();
  const classId = klass.id;
  const rosterKey = ["students", classId];

  const [name, setName] = useState("");
  const [gender, setGender] = useState<Gender | "">("");
  const [paste, setPaste] = useState("");

  const { data: students, isLoading } = useQuery({
    queryKey: rosterKey,
    queryFn: () => fetchStudents(classId),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: rosterKey });

  const addMutation = useMutation({
    mutationFn: () => addStudent(classId, name.trim(), gender || null),
    onSuccess: () => {
      setName("");
      setGender("");
      invalidate();
    },
  });

  const bulkMutation = useMutation({
    mutationFn: () => bulkAddStudents(classId, paste),
    onSuccess: () => {
      setPaste("");
      invalidate();
    },
  });

  const removeMutation = useMutation({
    mutationFn: (studentId: string) => removeStudent(classId, studentId),
    onSuccess: invalidate,
  });

  const purgeMutation = useMutation({
    mutationFn: (studentId: string) => purgeStudent(classId, studentId),
    onSuccess: invalidate,
  });

  const error =
    (addMutation.error as Error | null) ??
    (bulkMutation.error as Error | null) ??
    (removeMutation.error as Error | null) ??
    (purgeMutation.error as Error | null);

  return (
    <section className="roster">
      <h2>Roster — {klass.name}</h2>

      <form
        className="create-row"
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) addMutation.mutate();
        }}
      >
        <input
          aria-label="Student name"
          placeholder="Student name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <select
          aria-label="Gender"
          className="gender-select"
          value={gender}
          onChange={(e) => setGender(e.target.value as Gender | "")}
        >
          <option value="">—</option>
          <option value="Female">F</option>
          <option value="Male">M</option>
        </select>
        <button
          className="btn-primary inline"
          type="submit"
          disabled={addMutation.isPending || !name.trim()}
        >
          {addMutation.isPending ? "Adding…" : "Add"}
        </button>
      </form>

      <form
        className="paste-block"
        onSubmit={(e) => {
          e.preventDefault();
          if (paste.trim()) bulkMutation.mutate();
        }}
      >
        <label htmlFor="paste">Paste a list (one per line — "Name" or "Name, F/M")</label>
        <textarea
          id="paste"
          rows={4}
          placeholder={"Alice, F\nBob, M\nCharlie"}
          value={paste}
          onChange={(e) => setPaste(e.target.value)}
        />
        <button
          className="btn-primary inline"
          type="submit"
          disabled={bulkMutation.isPending || !paste.trim()}
        >
          {bulkMutation.isPending ? "Adding…" : "Add list"}
        </button>
      </form>

      {error && <p className="error">{error.message}</p>}

      {isLoading ? (
        <p className="empty">Loading roster…</p>
      ) : students && students.length > 0 ? (
        <ul className="class-list">
          {students.map((s) => (
            <RosterStudent
              key={s.id}
              classId={classId}
              student={s}
              onRemove={() => removeMutation.mutate(s.id)}
              removing={removeMutation.isPending}
              // Purge is the irreversible erasure path — Owner-only, mirroring the server's gate.
              canPurge={klass.role === "Owner"}
              onPurge={() => purgeMutation.mutate(s.id)}
              purging={purgeMutation.isPending}
            />
          ))}
        </ul>
      ) : (
        <p className="empty">No students yet — add one or paste a list above.</p>
      )}
    </section>
  );
}
