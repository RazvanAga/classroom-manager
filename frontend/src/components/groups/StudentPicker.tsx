"use client";

import { useMutation } from "@tanstack/react-query";
import { Clock, RotateCcw, Shuffle, Sparkles, Star } from "lucide-react";
import { useRef, useState } from "react";
import { pickStudent, type Student } from "@/lib/api";
import { ro } from "@/lib/strings";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";

// Random student picker (issue #24). Fairness is server-authoritative: the no-repeat-until-cycle logic
// lives in the backend FairPicker; the client only carries the already-picked set for the current cycle
// and replays it each turn. The spinning reveal here is pure presentation — names flash for a minimum
// duration while the request is in flight, then the wheel settles on the server's actual pick.

type Pick = { studentId: string; displayName: string };

const SPIN_TICK_MS = 80;
const MIN_SPIN_MS = 1500;

export function StudentPicker({ classId, students }: { classId: string; students: Student[] }) {
  const [alreadyPickedIds, setAlreadyPickedIds] = useState<string[]>([]);
  const [history, setHistory] = useState<Pick[]>([]);
  const [shown, setShown] = useState<Pick | null>(null);
  const [cycleReset, setCycleReset] = useState(false);
  const spinRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const pick = useMutation({
    mutationFn: async () => {
      const [res] = await Promise.all([
        pickStudent(classId, alreadyPickedIds),
        new Promise((r) => setTimeout(r, MIN_SPIN_MS)),
      ]);
      return res;
    },
    onMutate: () => {
      setCycleReset(false);
      // Flash random roster members so the wheel looks like it's spinning while we wait for the server.
      spinRef.current = setInterval(() => {
        const s = students[Math.floor(Math.random() * students.length)];
        if (s) setShown({ studentId: s.id, displayName: s.displayName });
      }, SPIN_TICK_MS);
    },
    onSuccess: (res) => {
      const picked: Pick = { studentId: res.pickedStudentId, displayName: res.pickedDisplayName };
      setShown(picked);
      setAlreadyPickedIds(res.alreadyPickedIds);
      setCycleReset(res.cycleReset);
      setHistory((prev) => [picked, ...prev.filter((p) => p.studentId !== picked.studentId)]);
    },
    onSettled: () => {
      if (spinRef.current) clearInterval(spinRef.current);
      spinRef.current = null;
    },
  });

  const spinning = pick.isPending;

  function resetCycle() {
    setAlreadyPickedIds([]);
    setHistory([]);
    setShown(null);
    setCycleReset(false);
  }

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-6 shadow-sm">
      <h2 className="mb-6 flex items-center gap-2 text-lg font-extrabold text-slate-800">
        <Shuffle size={20} className="text-violet-500" /> {ro.groups.picker.title}
      </h2>

      <div className="flex flex-col gap-6 md:flex-row">
        {/* Recent picks (newest first). The list doubles as the visible cycle state. */}
        <div className="flex-shrink-0 md:w-56">
          <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
            <p className="mb-3 flex items-center gap-1.5 text-xs font-extrabold uppercase tracking-wide text-slate-500">
              <Clock size={12} /> {ro.groups.picker.recent}
            </p>
            {history.length === 0 ? (
              <p className="text-xs italic text-slate-400">{ro.groups.picker.recentEmpty}</p>
            ) : (
              <ol className="space-y-1.5 overflow-y-auto pr-1" style={{ maxHeight: 180 }}>
                {history.map((p, idx) => (
                  <li key={p.studentId} className="flex items-center gap-2">
                    <span className="w-4 text-xs font-bold text-slate-400">{idx + 1}.</span>
                    <StudentAvatar classId={classId} studentId={p.studentId} size={26} className="rounded-lg" />
                    <span className="truncate text-sm font-medium text-slate-600">{p.displayName}</span>
                  </li>
                ))}
              </ol>
            )}
            {history.length > 0 && (
              <button
                type="button"
                onClick={resetCycle}
                className="mt-3 flex items-center gap-1.5 text-xs font-bold text-violet-600 transition hover:text-violet-800"
              >
                <RotateCcw size={12} /> {ro.groups.picker.reset}
              </button>
            )}
          </div>
        </div>

        {/* Spinner + button. */}
        <div className="flex flex-1 flex-col items-center justify-center gap-6">
          <div
            className={`relative flex h-52 w-52 items-center justify-center rounded-full border-4 shadow-lg transition-all duration-200 ${
              spinning
                ? "scale-105 border-violet-400 bg-violet-50 shadow-violet-200"
                : shown
                  ? "border-emerald-400 bg-gradient-to-br from-emerald-50 to-emerald-100 shadow-emerald-200"
                  : "border-slate-200 bg-slate-50"
            }`}
          >
            {shown && !spinning && (
              <>
                <Star size={16} className="absolute left-8 top-3 animate-bounce fill-yellow-400 text-yellow-400" />
                <Star size={12} className="absolute right-8 top-6 animate-bounce fill-yellow-300 text-yellow-300" style={{ animationDelay: "0.1s" }} />
                <Star size={14} className="absolute bottom-5 left-6 animate-bounce fill-amber-400 text-amber-400" style={{ animationDelay: "0.2s" }} />
                <Star size={10} className="absolute bottom-4 right-10 animate-bounce fill-yellow-400 text-yellow-400" style={{ animationDelay: "0.15s" }} />
              </>
            )}

            {shown ? (
              <div className="px-4 text-center">
                <StudentAvatar
                  classId={classId}
                  studentId={shown.studentId}
                  size={72}
                  className="mx-auto mb-2 rounded-2xl shadow"
                />
                <span className="text-base font-extrabold leading-tight text-slate-800">
                  {shown.displayName}
                </span>
              </div>
            ) : (
              <Star size={48} className="text-slate-200" />
            )}
          </div>

          <button
            type="button"
            onClick={() => pick.mutate()}
            disabled={spinning}
            className="flex items-center gap-3 rounded-2xl bg-gradient-to-r from-violet-600 to-indigo-500 px-8 py-4 text-base font-extrabold text-white shadow-lg shadow-purple-300 transition-all hover:opacity-90 active:scale-95 disabled:opacity-60"
          >
            <Sparkles size={20} />
            {spinning ? ro.groups.picker.picking : ro.groups.picker.pick}
          </button>

          {cycleReset && (
            <p className="text-center text-sm font-semibold text-amber-600">
              {ro.groups.picker.cycleReset}
            </p>
          )}
          {pick.isError && (
            <p className="text-center text-sm font-semibold text-rose-600">
              {(pick.error as Error).message}
            </p>
          )}
        </div>
      </div>
    </section>
  );
}
