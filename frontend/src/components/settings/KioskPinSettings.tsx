"use client";

import { useMutation } from "@tanstack/react-query";
import { Check, KeyRound } from "lucide-react";
import { useState } from "react";
import { setKioskPin } from "@/lib/api";
import { ro } from "@/lib/strings";
import { SettingsCard } from "./SettingsCard";

// The kiosk exit PIN setter (issue #26): a per-teacher numeric code (4–6 digits) used to leave kiosk
// mode. The server hashes it; we only ever send the new value. Format is validated server-side, so we
// keep the input to digits and let the endpoint reject anything out of range.
export function KioskPinSettings() {
  const [pin, setPin] = useState("");

  const save = useMutation({
    mutationFn: () => setKioskPin(pin),
    onSuccess: () => setPin(""),
  });

  const canSave = pin.length >= 4 && pin.length <= 6 && !save.isPending;

  return (
    <SettingsCard icon={KeyRound} title={ro.settings.pin.title} subtitle={ro.settings.pin.subtitle}>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (canSave) save.mutate();
        }}
        className="flex flex-wrap gap-2"
      >
        <input
          type="text"
          inputMode="numeric"
          value={pin}
          onChange={(e) => setPin(e.target.value.replace(/\D/g, "").slice(0, 6))}
          placeholder={ro.settings.pin.placeholder}
          className="min-w-[8rem] flex-1 rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm tracking-[0.3em] transition focus:border-violet-300 focus:bg-white focus:outline-none"
        />
        <button
          type="submit"
          disabled={!canSave}
          className="flex items-center gap-1.5 rounded-2xl bg-violet-600 px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-50"
        >
          <Check size={16} /> {save.isPending ? ro.settings.pin.saving : ro.settings.pin.save}
        </button>
      </form>

      <p className="mt-2 text-xs text-slate-400">{ro.settings.pin.hint}</p>

      {save.isError && (
        <p className="mt-2 text-sm font-semibold text-rose-600">{(save.error as Error).message}</p>
      )}
      {save.isSuccess && (
        <p className="mt-2 text-sm font-semibold text-emerald-600">{ro.settings.pin.saved}</p>
      )}
    </SettingsCard>
  );
}
