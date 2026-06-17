"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { createClass, fetchClasses, fetchMe, logout } from "@/lib/api";
import { RosterPanel } from "./RosterPanel";
import { BehaviorPanel } from "./BehaviorPanel";

export default function HomePage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const { data: me, isLoading } = useQuery({ queryKey: ["me"], queryFn: fetchMe });

  const { data: classes } = useQuery({
    queryKey: ["classes"],
    queryFn: fetchClasses,
    enabled: !!me,
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
    if (!isLoading && me === null) {
      router.replace("/login");
    }
  }, [isLoading, me, router]);

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

        {selected && <RosterPanel klass={selected} />}
        {selected && <BehaviorPanel classId={selected.id} />}
      </div>
    </main>
  );
}
