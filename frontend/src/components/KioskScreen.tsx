"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Lock, Star } from "lucide-react";
import { useState } from "react";
import { exitKiosk, type KioskSession } from "@/lib/api";
import { ro } from "@/lib/strings";

// A minimal, functional kiosk screen (slice #20). The kiosk session takes over the whole app: a
// reduced-scope principal can only browse, and leaving needs the teacher's PIN. The full student
// shop/equip kiosk experience is redesigned in slice #27 (#27 Blocked by #22, #23).
export function KioskScreen({ session }: { session: KioskSession }) {
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
    <main className="grid min-h-screen place-items-center p-6">
      <div className="w-full max-w-md rounded-3xl bg-white p-8 text-center shadow-xl shadow-purple-900/10 ring-1 ring-violet-100">
        <div className="mx-auto mb-4 flex h-14 w-14 rotate-3 items-center justify-center rounded-2xl bg-yellow-400 shadow-md">
          <Star size={28} className="fill-yellow-900 text-yellow-900" />
        </div>
        <span className="inline-block rounded-full bg-violet-100 px-3 py-1 text-xs font-bold uppercase tracking-wide text-violet-700">
          {ro.kiosk.badge}
        </span>
        <h1 className="mt-3 text-2xl font-extrabold tracking-tight">{session.className}</h1>
        <p className="mt-2 text-sm text-slate-500">{ro.kiosk.description}</p>

        <form
          className="mt-6 flex flex-col gap-3"
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
              aria-label={ro.kiosk.pinPlaceholder}
              placeholder={ro.kiosk.pinPlaceholder}
              value={pin}
              onChange={(e) => setPin(e.target.value)}
              className="w-full bg-transparent py-3 text-lg outline-none"
            />
          </div>
          <button
            type="submit"
            disabled={exitMutation.isPending || !pin.trim()}
            className="rounded-2xl bg-violet-600 py-3 text-lg font-bold text-white shadow-lg shadow-violet-600/30 transition hover:bg-violet-700 disabled:opacity-50"
          >
            {exitMutation.isPending ? ro.kiosk.exiting : ro.kiosk.exit}
          </button>
          {exitMutation.isError && (
            <p className="text-sm font-medium text-rose-600">{ro.kiosk.wrongPin}</p>
          )}
        </form>
      </div>
    </main>
  );
}
