"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Archive, Users } from "lucide-react";
import { useRouter } from "next/navigation";
import { archiveClass, fetchStudents, type ClassSummary } from "@/lib/api";
import { glyphFor } from "@/lib/currency";
import { ro } from "@/lib/strings";

// A single class on the selector grid: name, role, student count and the class's currency glyph.
// Clicking the card enters the class (its Dashboard); the archive button is kept separate so a card
// click never archives by accident. Student count is derived client-side (the roster endpoint) so no
// backend field is needed for it.
export function ClassCard({ klass }: { klass: ClassSummary }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const Glyph = glyphFor(klass.currencyIcon);

  const { data: students } = useQuery({
    queryKey: ["students", klass.id],
    queryFn: () => fetchStudents(klass.id),
  });

  const archiveMutation = useMutation({
    mutationFn: () => archiveClass(klass.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["classes"] }),
  });

  const enter = () => router.push(`/dashboard?class=${klass.id}`);

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={enter}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          enter();
        }
      }}
      className="group flex cursor-pointer flex-col gap-4 rounded-3xl bg-white p-6 text-left shadow-lg shadow-purple-900/5 ring-1 ring-violet-100 transition hover:-translate-y-0.5 hover:shadow-xl hover:ring-violet-300"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex h-12 w-12 rotate-3 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-500 text-white shadow-md">
          <Glyph size={24} className="fill-yellow-300 text-yellow-300" />
        </div>
        <span className="rounded-full bg-violet-100 px-3 py-1 text-xs font-bold uppercase tracking-wide text-violet-700">
          {klass.role === "Owner" ? ro.classes.roleOwner : ro.classes.roleCollaborator}
        </span>
      </div>

      <h2 className="text-xl font-extrabold leading-tight tracking-tight">{klass.name}</h2>

      <div className="mt-auto flex items-center justify-between">
        <span className="flex items-center gap-1.5 text-sm font-semibold text-slate-500">
          <Users size={16} />
          {ro.classes.students(students?.length ?? 0)}
        </span>
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            if (confirm(ro.classes.archiveConfirm)) archiveMutation.mutate();
          }}
          disabled={archiveMutation.isPending}
          aria-label={ro.classes.archive}
          className="flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-sm font-semibold text-slate-400 transition hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50"
        >
          <Archive size={16} />
          {archiveMutation.isPending ? ro.classes.archiving : ro.classes.archive}
        </button>
      </div>
    </div>
  );
}
