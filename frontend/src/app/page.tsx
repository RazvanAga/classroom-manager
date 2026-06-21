"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Star } from "lucide-react";
import { useState } from "react";
import { createClass, fetchClasses, fetchMe, logout } from "@/lib/api";
import { ClassCard } from "@/components/ClassCard";
import { ro } from "@/lib/strings";

// The root page is the class selector (slice #20): the teacher's classes as cards, with create +
// archive. Selecting a card routes into that class. Auth/kiosk gating is handled by <AuthGate>, so
// this renders only for a signed-in teacher.
export default function ClassSelectorPage() {
  const queryClient = useQueryClient();
  const [name, setName] = useState("");

  const { data: me } = useQuery({ queryKey: ["me"], queryFn: fetchMe });
  const { data: classes } = useQuery({ queryKey: ["classes"], queryFn: fetchClasses });

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

  return (
    <main className="mx-auto max-w-5xl px-4 py-8 sm:py-12">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 rotate-3 items-center justify-center rounded-2xl bg-yellow-400 shadow-md">
            <Star size={26} className="fill-yellow-900 text-yellow-900" />
          </div>
          <div>
            <h1 className="text-3xl font-extrabold tracking-tight">{ro.classes.title}</h1>
            {me && (
              <p className="text-sm text-slate-500">
                {ro.classes.signedInAs} <span className="font-semibold text-violet-700">{me.displayName}</span>
              </p>
            )}
          </div>
        </div>
        <button
          type="button"
          onClick={() => logoutMutation.mutate()}
          disabled={logoutMutation.isPending}
          className="rounded-xl px-4 py-2 text-sm font-semibold text-slate-500 transition hover:bg-slate-100 hover:text-slate-800 disabled:opacity-50"
        >
          {logoutMutation.isPending ? ro.auth.signingOut : ro.auth.signOut}
        </button>
      </header>

      <form
        className="mt-8 flex gap-3"
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) createMutation.mutate();
        }}
      >
        <input
          aria-label={ro.classes.newClassPlaceholder}
          placeholder={ro.classes.newClassPlaceholder}
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-lg outline-none focus:border-violet-400"
        />
        <button
          type="submit"
          disabled={createMutation.isPending || !name.trim()}
          className="flex shrink-0 items-center gap-2 rounded-2xl bg-violet-600 px-5 py-3 text-lg font-bold text-white shadow-lg shadow-violet-600/30 transition hover:bg-violet-700 disabled:opacity-50"
        >
          <Plus size={20} />
          <span className="hidden sm:inline">{createMutation.isPending ? ro.classes.adding : ro.classes.add}</span>
        </button>
      </form>
      {createMutation.isError && (
        <p className="mt-3 text-sm font-medium text-rose-600">{(createMutation.error as Error).message}</p>
      )}

      {classes && classes.length > 0 ? (
        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {classes.map((c) => (
            <ClassCard key={c.id} klass={c} />
          ))}
        </div>
      ) : (
        <p className="mt-12 text-center text-lg text-slate-400">{ro.classes.empty}</p>
      )}
    </main>
  );
}
