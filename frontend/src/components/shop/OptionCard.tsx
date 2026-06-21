"use client";

import { Check, Sparkles } from "lucide-react";
import type { AvatarRarity, EquippedSlot } from "@/lib/api";
import { avatarDataUri } from "@/lib/avatar";
import type { CurrencyIcon } from "@/lib/currency";
import { borderFor, chipFor, isLegendary } from "@/lib/rarity";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";

export type OptionState = "equipped" | "owned" | "buy" | "insufficient";

// One store option: a live preview of the student's avatar with *this* option swapped in (so colors and
// shapes are judged on the actual face), a rarity accent frame, and a state-driven footer. Legendary
// gets a distinct gold frame; the rest get a tinted border.
export function OptionCard({
  seed,
  previewEquipped,
  displayName,
  rarity,
  cost,
  state,
  currencyIcon,
  pending,
  onEquip,
  onBuy,
}: {
  seed: string;
  previewEquipped: EquippedSlot[];
  displayName: string;
  rarity: AvatarRarity | null;
  cost: number;
  state: OptionState;
  currencyIcon: CurrencyIcon;
  pending: boolean;
  onEquip: () => void;
  onBuy: () => void;
}) {
  const legendary = isLegendary(rarity);
  const preview = avatarDataUri(seed, previewEquipped);

  const card = (
    <div
      className={`flex h-full flex-col gap-2 rounded-2xl border-2 bg-white p-2.5 ${borderFor(rarity)} ${
        state === "equipped" ? "ring-2 ring-violet-400" : ""
      }`}
    >
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-violet-50 to-indigo-50">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={preview} alt="" width={96} height={96} className="mx-auto h-24 w-24" />
        <span
          className={`absolute left-1 top-1 flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-bold ${chipFor(
            rarity,
          )}`}
        >
          {legendary && <Sparkles size={9} />}
          {ro.shop.rarity[rarity ?? "Common"]}
        </span>
      </div>

      <p className="truncate text-center text-xs font-bold text-slate-600">{displayName}</p>

      <Footer
        state={state}
        cost={cost}
        currencyIcon={currencyIcon}
        pending={pending}
        onEquip={onEquip}
        onBuy={onBuy}
      />
    </div>
  );

  // Legendary's distinct gold frame: a gradient border created by a thin padded wrapper.
  if (legendary) {
    return (
      <div className="rounded-[18px] bg-gradient-to-br from-amber-300 via-yellow-300 to-orange-400 p-[2px] shadow-md shadow-amber-200/60">
        {card}
      </div>
    );
  }
  return card;
}

function Footer({
  state,
  cost,
  currencyIcon,
  pending,
  onEquip,
  onBuy,
}: {
  state: OptionState;
  cost: number;
  currencyIcon: CurrencyIcon;
  pending: boolean;
  onEquip: () => void;
  onBuy: () => void;
}) {
  if (state === "equipped") {
    return (
      <span className="flex items-center justify-center gap-1 rounded-xl bg-violet-100 py-1.5 text-xs font-bold text-violet-700">
        <Check size={13} strokeWidth={3} /> {ro.shop.equipped}
      </span>
    );
  }

  if (state === "owned") {
    return (
      <button
        type="button"
        onClick={onEquip}
        disabled={pending}
        className="rounded-xl bg-violet-600 py-1.5 text-xs font-bold text-white transition hover:bg-violet-700 active:scale-95 disabled:opacity-50"
      >
        {pending ? ro.shop.equipping : ro.shop.equip}
      </button>
    );
  }

  if (state === "buy") {
    return (
      <button
        type="button"
        onClick={onBuy}
        disabled={pending}
        className="flex items-center justify-center gap-1.5 rounded-xl bg-emerald-500 py-1.5 text-xs font-bold text-white transition hover:bg-emerald-600 active:scale-95 disabled:opacity-50"
      >
        {ro.shop.buy}
        <Points value={cost} icon={currencyIcon} size={11} tone="neutral" showSign={false} />
      </button>
    );
  }

  return (
    <span className="flex items-center justify-center gap-1.5 rounded-xl bg-slate-100 py-1.5 text-xs font-bold text-slate-400">
      <Points value={cost} icon={currencyIcon} size={11} tone="neutral" showSign={false} />
    </span>
  );
}
