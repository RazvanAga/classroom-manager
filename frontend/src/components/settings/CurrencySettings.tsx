"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Coins, Loader2 } from "lucide-react";
import {
  type CurrencyIcon,
  currencyIcons,
  currencyLabels,
  glyphFor,
} from "@/lib/currency";
import { setCurrencyIcon } from "@/lib/api";
import { ro } from "@/lib/strings";
import { SettingsCard } from "./SettingsCard";

// The reward-currency picker (issue #26, wired to #19's field): choose one glyph from the fixed set.
// The selection is shared class config — any member may change it; the server validates the value.
export function CurrencySettings({
  classId,
  current,
}: {
  classId: string;
  current: CurrencyIcon;
}) {
  const queryClient = useQueryClient();

  const choose = useMutation({
    mutationFn: (icon: CurrencyIcon) => setCurrencyIcon(classId, icon),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["classes"] }),
  });

  const pending = choose.isPending ? choose.variables : null;

  return (
    <SettingsCard
      icon={Coins}
      title={ro.settings.currency.title}
      subtitle={ro.settings.currency.subtitle}
    >
      <div className="grid grid-cols-5 gap-2">
        {currencyIcons.map((icon) => {
          const Glyph = glyphFor(icon);
          const active = icon === current;
          const isPending = pending === icon;
          return (
            <button
              key={icon}
              type="button"
              onClick={() => !active && choose.mutate(icon)}
              disabled={choose.isPending}
              title={currencyLabels[icon]}
              aria-label={currencyLabels[icon]}
              className={`flex aspect-square items-center justify-center rounded-2xl border-2 transition disabled:cursor-default ${
                active
                  ? "border-violet-500 bg-violet-50 shadow-inner"
                  : "border-slate-100 bg-slate-50 hover:border-violet-300 hover:bg-white"
              }`}
            >
              {isPending ? (
                <Loader2 size={22} className="animate-spin text-violet-400" />
              ) : (
                <Glyph
                  size={24}
                  className={active ? "fill-amber-300 text-amber-400" : "text-slate-400"}
                />
              )}
            </button>
          );
        })}
      </div>

      {choose.isError && (
        <p className="mt-3 text-sm font-semibold text-rose-600">{(choose.error as Error).message}</p>
      )}
    </SettingsCard>
  );
}
