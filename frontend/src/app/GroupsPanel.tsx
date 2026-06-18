"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  fetchGrouping,
  fetchGroupings,
  previewGrouping,
  saveGrouping,
  type FormedGroup,
} from "@/lib/api";

// Random group maker for the selected class: form even (optionally gender-balanced) groups, then
// optionally save the arrangement to reuse. Forming and saving share a seed so the saved grouping is
// exactly what was previewed (the server re-derives it deterministically — design.md §6.3).
export function GroupsPanel({ classId }: { classId: string }) {
  const queryClient = useQueryClient();
  const savedKey = ["groupings", classId];

  const [groupSize, setGroupSize] = useState("2");
  const [balance, setBalance] = useState(false);
  const [name, setName] = useState("");

  // What's on screen: a freshly-formed (savable) arrangement carries its seed; a loaded saved one
  // carries a title instead.
  const [groups, setGroups] = useState<FormedGroup[] | null>(null);
  const [seed, setSeed] = useState<number | null>(null);
  const [savedTitle, setSavedTitle] = useState<string | null>(null);

  const { data: saved } = useQuery({ queryKey: savedKey, queryFn: () => fetchGroupings(classId) });

  const size = Math.max(1, Number(groupSize) || 1);

  const form = useMutation({
    mutationFn: () => previewGrouping(classId, size, balance),
    onSuccess: (result) => {
      setGroups(result.groups);
      setSeed(result.seed);
      setSavedTitle(null);
    },
  });

  const save = useMutation({
    mutationFn: () => saveGrouping(classId, name.trim() || null, size, balance, seed!),
    onSuccess: (result) => {
      setName("");
      setSavedTitle(result.name ?? "Saved grouping");
      setSeed(null); // saved — no longer a fresh, re-savable arrangement
      queryClient.invalidateQueries({ queryKey: savedKey });
    },
  });

  const load = useMutation({
    mutationFn: (groupingId: string) => fetchGrouping(classId, groupingId),
    onSuccess: (result) => {
      setGroups(result.groups);
      setSeed(null);
      setSavedTitle(result.name ?? "Saved grouping");
    },
  });

  const error =
    (form.error as Error | null) ?? (save.error as Error | null) ?? (load.error as Error | null);

  return (
    <section className="roster">
      <h2>Group maker</h2>

      <div className="groups-controls">
        <label className="groups-size">
          Students per group
          <input
            aria-label="Students per group"
            className="points-input"
            type="number"
            min={1}
            value={groupSize}
            onChange={(e) => setGroupSize(e.target.value)}
          />
        </label>
        <label className="groups-balance">
          <input
            type="checkbox"
            className="pick-box"
            checked={balance}
            onChange={(e) => setBalance(e.target.checked)}
          />
          Balance by gender
        </label>
        <button
          className="btn-primary inline"
          onClick={() => form.mutate()}
          disabled={form.isPending}
        >
          {form.isPending ? "Forming…" : "Make groups"}
        </button>
      </div>

      {error && <p className="error">{error.message}</p>}

      {groups && (
        <>
          <div className="groups-head">
            <h3 className="groups-title">{savedTitle ?? "Formed groups"}</h3>
            {seed !== null && (
              <form
                className="groups-save"
                onSubmit={(e) => {
                  e.preventDefault();
                  save.mutate();
                }}
              >
                <input
                  aria-label="Grouping name"
                  placeholder="Name (optional)"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
                <button className="btn-primary inline" type="submit" disabled={save.isPending}>
                  {save.isPending ? "Saving…" : "Save"}
                </button>
              </form>
            )}
          </div>

          <div className="groups-grid">
            {groups.map((group) => (
              <div className="group-card" key={group.groupNumber}>
                <span className="group-card-title">Group {group.groupNumber + 1}</span>
                <ul className="group-members">
                  {group.members.map((m) => (
                    <li key={m.studentId}>
                      {m.displayName}
                      {m.gender && <span className="group-gender">{m.gender[0]}</span>}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </>
      )}

      {saved && saved.length > 0 && (
        <>
          <h3 className="groups-title">Saved groupings</h3>
          <ul className="class-list">
            {saved.map((g) => (
              <li
                key={g.id}
                className="class-item selectable"
                onClick={() => load.mutate(g.id)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    load.mutate(g.id);
                  }
                }}
              >
                <span className="class-name">{g.name ?? "Untitled grouping"}</span>
                <span className="activity-label">
                  {g.groupCount} groups · {g.studentCount} students
                  {g.balancedByGender ? " · gender-balanced" : ""}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}
