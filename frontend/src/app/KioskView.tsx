"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  exitKiosk,
  fetchStudentAvatar,
  fetchStudents,
  type KioskSession,
} from "@/lib/api";
import { StudentShop, avatarUri } from "./StudentShop";

// A single tappable student tile: composed avatar + name, no teacher controls.
function StudentTile({
  classId,
  studentId,
  name,
  onPick,
}: {
  classId: string;
  studentId: string;
  name: string;
  onPick: () => void;
}) {
  const { data: avatar } = useQuery({
    queryKey: ["avatar", classId, studentId],
    queryFn: () => fetchStudentAvatar(classId, studentId),
  });
  const uri = useMemo(
    () => (avatar ? avatarUri(studentId, avatar.equipped, 96) : null),
    [avatar, studentId],
  );

  return (
    <button className="kiosk-tile" onClick={onPick}>
      {uri ? (
        <img className="avatar-thumb" src={uri} width={64} height={64} alt="" />
      ) : (
        <span className="avatar-thumb placeholder" aria-hidden />
      )}
      <span className="kiosk-tile-name">{name}</span>
    </button>
  );
}

// The kiosk experience: a class roster of names a student taps to open their own shop/equip. No
// teacher actions are present (and the server rejects them for the kiosk principal anyway,
// design.md §5.4). Exiting requires the teacher's PIN, which restores the full teacher session.
export function KioskView({ session }: { session: KioskSession }) {
  const queryClient = useQueryClient();
  const classId = session.classId;
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [pin, setPin] = useState("");
  const [unlocking, setUnlocking] = useState(false);

  const { data: students, isLoading } = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });

  const selected = students?.find((s) => s.id === selectedId) ?? null;

  const exit = useMutation({
    mutationFn: () => exitKiosk(pin.trim()),
    onSuccess: () => {
      setPin("");
      setUnlocking(false);
      // The server restored the teacher session; refresh both "who am I" queries so the app
      // re-renders the teacher view.
      queryClient.invalidateQueries({ queryKey: ["kioskSession"] });
      queryClient.invalidateQueries({ queryKey: ["me"] });
    },
  });

  return (
    <main className="shell">
      <div className="card wide">
        <header className="row">
          <div>
            <h1>{session.className}</h1>
            <p className="subtitle">
              Kiosk mode — tap your name to shop &amp; customize your avatar
            </p>
          </div>
          {unlocking ? (
            <form
              className="kiosk-exit"
              onSubmit={(e) => {
                e.preventDefault();
                if (pin.trim()) exit.mutate();
              }}
            >
              <input
                aria-label="Exit PIN"
                inputMode="numeric"
                type="password"
                placeholder="PIN"
                autoFocus
                value={pin}
                onChange={(e) => setPin(e.target.value)}
              />
              <button className="btn-primary inline" type="submit" disabled={exit.isPending || !pin.trim()}>
                {exit.isPending ? "Checking…" : "Exit"}
              </button>
              <button
                type="button"
                className="btn-ghost"
                onClick={() => {
                  setUnlocking(false);
                  setPin("");
                  exit.reset();
                }}
              >
                Cancel
              </button>
            </form>
          ) : (
            <button className="btn-ghost" onClick={() => setUnlocking(true)}>
              Exit kiosk
            </button>
          )}
        </header>

        {exit.isError && <p className="error">{(exit.error as Error).message}</p>}

        {selected ? (
          <section className="roster">
            <header className="row">
              <h2>{selected.displayName}</h2>
              <button className="btn-ghost" onClick={() => setSelectedId(null)}>
                ← All students
              </button>
            </header>
            <StudentShop classId={classId} studentId={selected.id} />
          </section>
        ) : isLoading ? (
          <p className="empty">Loading…</p>
        ) : students && students.length > 0 ? (
          <div className="kiosk-grid">
            {students.map((s) => (
              <StudentTile
                key={s.id}
                classId={classId}
                studentId={s.id}
                name={s.displayName}
                onPick={() => setSelectedId(s.id)}
              />
            ))}
          </div>
        ) : (
          <p className="empty">No students in this class yet.</p>
        )}
      </div>
    </main>
  );
}
