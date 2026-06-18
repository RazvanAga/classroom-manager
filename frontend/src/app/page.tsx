"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  createClass,
  enterKiosk,
  fetchClasses,
  fetchKioskSession,
  fetchMe,
  logout,
} from "@/lib/api";
import { RosterPanel } from "./RosterPanel";
import { BehaviorPanel } from "./BehaviorPanel";
import { PointsPanel } from "./PointsPanel";
import { PickerPanel } from "./PickerPanel";
import { GroupsPanel } from "./GroupsPanel";
import { ReportsPanel } from "./ReportsPanel";
import { TimerPanel } from "./TimerPanel";
import { KioskView } from "./KioskView";

export default function HomePage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // A kiosk session takes over the whole app, so check for it first. While it loads we hold off on
  // the teacher queries (and the login redirect) to avoid a flash of the wrong view.
  const { data: kiosk, isLoading: kioskLoading } = useQuery({
    queryKey: ["kioskSession"],
    queryFn: fetchKioskSession,
  });

  const { data: me, isLoading: meLoading } = useQuery({
    queryKey: ["me"],
    queryFn: fetchMe,
    enabled: !kioskLoading && !kiosk,
  });
  const isLoading = kioskLoading || (!kiosk && meLoading);

  const { data: classes } = useQuery({
    queryKey: ["classes"],
    queryFn: fetchClasses,
    enabled: !!me,
  });

  const enterKioskMutation = useMutation({
    mutationFn: (classId: string) => enterKiosk(classId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["kioskSession"] }),
  });

  const selected = classes?.find((c) => c.id === selectedId) ?? null;

  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => queryClient.setQueryData(["me"], null),
  });

  const createMutation = useMutation({
    mutationFn: () => createClass(name.trim()),
    onSuccess: () => {
      setName("");
      queryClient.invalidateQueries({ queryKey: ["classes"] });
    },
  });

  useEffect(() => {
    if (!isLoading && !kiosk && me === null) {
      router.replace("/login");
    }
  }, [isLoading, kiosk, me, router]);

  if (kiosk) {
    return <KioskView session={kiosk} />;
  }

  if (isLoading || !me) {
    return (
      <main className="shell">
        <div className="card greeting">Loading…</div>
      </main>
    );
  }

  return (
    <main className="shell">
      <div className="card wide">
        <header className="row">
          <div>
            <h1>Classes</h1>
            <p className="subtitle">
              Signed in as <span className="name">{me.displayName}</span>
            </p>
          </div>
          <button
            className="btn-ghost"
            onClick={() => logoutMutation.mutate()}
            disabled={logoutMutation.isPending}
          >
            {logoutMutation.isPending ? "Signing out…" : "Sign out"}
          </button>
        </header>

        <form
          className="create-row"
          onSubmit={(e) => {
            e.preventDefault();
            if (name.trim()) createMutation.mutate();
          }}
        >
          <input
            aria-label="New class name"
            placeholder="New class name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <button
            className="btn-primary inline"
            type="submit"
            disabled={createMutation.isPending || !name.trim()}
          >
            {createMutation.isPending ? "Adding…" : "Add"}
          </button>
        </form>

        {createMutation.isError && (
          <p className="error">{(createMutation.error as Error).message}</p>
        )}

        {classes && classes.length > 0 ? (
          <ul className="class-list">
            {classes.map((c) => (
              <li
                key={c.id}
                className={`class-item selectable${c.id === selectedId ? " selected" : ""}`}
                onClick={() => setSelectedId(c.id === selectedId ? null : c.id)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    setSelectedId(c.id === selectedId ? null : c.id);
                  }
                }}
              >
                <span className="class-name">{c.name}</span>
                <span className="role-tag">{c.role}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty">No classes yet — create your first one above.</p>
        )}

        {selected && (
          <div className="kiosk-launch">
            <button
              className="btn-ghost"
              onClick={() => enterKioskMutation.mutate(selected.id)}
              disabled={enterKioskMutation.isPending}
            >
              {enterKioskMutation.isPending ? "Entering…" : `Enter kiosk mode for ${selected.name}`}
            </button>
            {enterKioskMutation.isError && (
              <p className="error">{(enterKioskMutation.error as Error).message}</p>
            )}
          </div>
        )}

        {selected && <RosterPanel klass={selected} />}
        {selected && <BehaviorPanel classId={selected.id} />}
        {selected && <PointsPanel classId={selected.id} />}
        {selected && <ReportsPanel classId={selected.id} />}
        {selected && <PickerPanel classId={selected.id} />}
        {selected && <GroupsPanel classId={selected.id} />}
        {selected && <TimerPanel />}
      </div>
    </main>
  );
}
