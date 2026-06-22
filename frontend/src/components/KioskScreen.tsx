"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Lock, LogOut, Users, X } from "lucide-react";
import { useMemo, useState } from "react";
import {
  type KioskSession,
  type LeaderboardEntry,
  exitKiosk,
  fetchLeaderboard,
  fetchStudents,
} from "@/lib/api";
import { type CurrencyIcon, glyphFor } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";
import { StudentShop } from "@/components/shop/StudentShop";

// The kid-facing kiosk (issue #27): a locked-down, projector/tablet-friendly view over the existing
// reduced-scope kiosk principal. A big avatar grid; tapping a student opens their personal area (stars
// + avatar + the store to browse/buy/equip — the shared StudentShop). An understated read-only
// leaderboard sits alongside. No teacher/admin controls are present (and the server rejects them for the
// kiosk principal anyway). Leaving needs the teacher's PIN, which restores the full teacher session.
export function KioskScreen({ session }: { session: KioskSession }) {
  const { classId, className, currencyIcon } = session;
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [exiting, setExiting] = useState(false);

  const students = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });
  const leaderboard = useQuery({
    queryKey: ["leaderboard", classId],
    queryFn: () => fetchLeaderboard(classId),
  });

  const walletByStudent = useMemo(() => {
    const map = new Map<string, number>();
    for (const e of leaderboard.data ?? []) map.set(e.studentId, e.wallet);
    return map;
  }, [leaderboard.data]);

  const roster = useMemo(
    () => [...(students.data ?? [])].sort((a, b) => a.displayName.localeCompare(b.displayName)),
    [students.data],
  );

  const selected = roster.find((s) => s.id === selectedId) ?? null;
  const Glyph = glyphFor(currencyIcon);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="bg-gradient-to-r from-violet-700 via-purple-600 to-indigo-600 shadow-lg shadow-purple-900/30">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-3 px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 rotate-3 items-center justify-center rounded-2xl bg-white/15 text-white shadow-md">
              <Glyph size={26} className="fill-yellow-300 text-yellow-300" />
            </div>
            <div>
              <h1 className="text-2xl font-extrabold tracking-tight text-white">{className}</h1>
              <span className="text-xs font-bold uppercase tracking-wide text-purple-200">
                {ro.kiosk.badge}
              </span>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setExiting(true)}
            className="flex items-center gap-2 rounded-xl border border-white/30 bg-white/10 px-4 py-2.5 text-sm font-bold text-white transition hover:bg-white/20"
          >
            <Lock size={16} /> {ro.kiosk.exit}
          </button>
        </div>
      </header>

      <main className="mx-auto w-full max-w-7xl flex-1 px-6 py-8">
        {students.isLoading || leaderboard.isLoading ? (
          <div className="grid place-items-center py-24 text-indigo-900/60">
            <Loader2 className="animate-spin" size={32} />
          </div>
        ) : selected ? (
          <StudentShop
            classId={classId}
            studentId={selected.id}
            studentName={selected.displayName}
            currencyIcon={currencyIcon}
            onBack={() => setSelectedId(null)}
          />
        ) : roster.length === 0 ? (
          <div className="rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-20 text-center">
            <Users size={48} className="mx-auto text-violet-300" />
            <p className="mt-4 text-xl font-bold text-slate-600">{ro.kiosk.emptyRoster}</p>
          </div>
        ) : (
          <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
            <section>
              <p className="mb-5 text-center text-lg font-bold text-slate-500 lg:text-left">
                {ro.kiosk.pickStudent}
              </p>
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
                {roster.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => setSelectedId(s.id)}
                    className="group flex cursor-pointer flex-col items-center gap-2.5 rounded-3xl border-2 border-white bg-white p-5 shadow-md shadow-purple-100/60 transition-all duration-150 hover:scale-105 hover:border-violet-300 hover:shadow-xl focus:outline-none focus-visible:border-violet-400"
                  >
                    <StudentAvatar classId={classId} studentId={s.id} size={104} />
                    <span className="line-clamp-2 text-center text-lg font-extrabold leading-tight text-slate-700">
                      {s.displayName.split(" ")[0]}
                    </span>
                    <Points
                      value={walletByStudent.get(s.id) ?? 0}
                      icon={currencyIcon}
                      size={16}
                      tone="neutral"
                      showSign={false}
                    />
                  </button>
                ))}
              </div>
            </section>

            <KioskLeaderboard entries={leaderboard.data ?? []} currencyIcon={currencyIcon} />
          </div>
        )}
      </main>

      {exiting && <ExitDialog onClose={() => setExiting(false)} />}
    </div>
  );
}

// An understated, read-only ranking by lifetime earned. Mirrors the dashboard leaderboard's data but
// stays quiet on the kiosk: no medals, no podium tiles — a plain numbered list the class can glance at.
function KioskLeaderboard({
  entries,
  currencyIcon,
}: {
  entries: LeaderboardEntry[];
  currencyIcon: CurrencyIcon;
}) {
  const ranked = entries.filter((e) => e.lifetimeEarned > 0).slice(0, 10);

  return (
    <aside className="lg:sticky lg:top-6 lg:self-start">
      <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
        <header className="mb-4">
          <h2 className="font-extrabold text-slate-800">{ro.kiosk.leaderboardTitle}</h2>
          <span className="text-xs font-semibold text-slate-400">{ro.kiosk.leaderboardSubtitle}</span>
        </header>
        {ranked.length === 0 ? (
          <p className="py-6 text-center text-sm text-slate-400">{ro.leaderboard.empty}</p>
        ) : (
          <ol className="space-y-1">
            {ranked.map((entry, i) => (
              <li
                key={entry.studentId}
                className="flex items-center gap-3 rounded-xl px-2 py-1.5"
              >
                <span className="w-5 text-right text-xs font-bold text-slate-400">{i + 1}</span>
                <span className="flex-1 truncate text-sm font-semibold text-slate-600">
                  {entry.displayName}
                </span>
                <Points
                  value={entry.lifetimeEarned}
                  icon={currencyIcon}
                  size={12}
                  tone="neutral"
                  showSign={false}
                />
              </li>
            ))}
          </ol>
        )}
      </section>
    </aside>
  );
}

// The PIN-gated exit (design.md §5.4): a correct PIN tears down the kiosk session server-side and
// restores the teacher; a wrong one keeps the class locked. The queries are invalidated so AuthGate
// re-resolves into the full teacher app.
function ExitDialog({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [pin, setPin] = useState("");

  const exitMutation = useMutation({
    mutationFn: () => exitKiosk(pin),
    onSuccess: () => {
      setPin("");
      queryClient.invalidateQueries({ queryKey: ["kioskSession"] });
      queryClient.invalidateQueries({ queryKey: ["me"] });
    },
  });

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-indigo-950/40 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="w-full max-w-sm rounded-3xl bg-white p-7 text-center shadow-2xl shadow-purple-900/20"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label={ro.kiosk.cancel}
          className="ml-auto flex h-9 w-9 items-center justify-center rounded-full text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
        >
          <X size={18} />
        </button>
        <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-violet-100">
          <LogOut size={26} className="text-violet-600" />
        </div>
        <h2 className="text-xl font-extrabold tracking-tight text-slate-800">{ro.kiosk.exitTitle}</h2>
        <p className="mt-1 text-sm text-slate-500">{ro.kiosk.exitHint}</p>

        <form
          className="mt-5 flex flex-col gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            if (pin.trim()) exitMutation.mutate();
          }}
        >
          <div className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-4 focus-within:border-violet-400">
            <Lock size={18} className="text-slate-400" />
            <input
              type="password"
              inputMode="numeric"
              autoComplete="off"
              autoFocus
              aria-label={ro.kiosk.pinPlaceholder}
              placeholder={ro.kiosk.pinPlaceholder}
              value={pin}
              onChange={(e) => setPin(e.target.value)}
              className="w-full bg-transparent py-3 text-center text-2xl tracking-widest outline-none"
            />
          </div>
          <button
            type="submit"
            disabled={exitMutation.isPending || !pin.trim()}
            className="rounded-2xl bg-violet-600 py-3 text-lg font-bold text-white shadow-lg shadow-violet-600/30 transition hover:bg-violet-700 disabled:opacity-50"
          >
            {exitMutation.isPending ? ro.kiosk.exiting : ro.kiosk.exitTitle}
          </button>
          {exitMutation.isError && (
            <p className="text-sm font-medium text-rose-600">{ro.kiosk.wrongPin}</p>
          )}
        </form>
      </div>
    </div>
  );
}
