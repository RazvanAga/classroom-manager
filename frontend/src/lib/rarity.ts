import type { AvatarRarity } from "./api";

// Rarity as a professional visual accent (issue #23): one frame per tier, Common → Legendary, with
// Legendary given a distinct gold frame. The DiceBear style stays locked — the "fun" comes from this
// polish, not new avatar data. Null-rarity options fall back to the Common treatment.

// Border accent for the option tile. Legendary is handled separately (a gradient frame wrapper), so
// here it only needs a transparent border to sit inside that frame.
export const rarityBorder: Record<AvatarRarity, string> = {
  Common: "border-slate-200",
  Rare: "border-sky-300",
  Epic: "border-violet-300",
  Legendary: "border-transparent",
};

// The little rarity label chip.
export const rarityChip: Record<AvatarRarity, string> = {
  Common: "bg-slate-100 text-slate-500",
  Rare: "bg-sky-100 text-sky-700",
  Epic: "bg-violet-100 text-violet-700",
  Legendary: "bg-gradient-to-r from-amber-300 to-orange-400 text-amber-950",
};

export function isLegendary(rarity: AvatarRarity | null): boolean {
  return rarity === "Legendary";
}

export function borderFor(rarity: AvatarRarity | null): string {
  return rarity ? rarityBorder[rarity] : rarityBorder.Common;
}

export function chipFor(rarity: AvatarRarity | null): string {
  return rarity ? rarityChip[rarity] : rarityChip.Common;
}
