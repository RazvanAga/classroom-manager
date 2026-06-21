import {
  Apple,
  Candy,
  Coins,
  Crown,
  Flower2,
  Gem,
  Heart,
  Rainbow,
  Star,
  Trophy,
  type LucideIcon,
} from "lucide-react";

// The per-class reward-currency icon (slice #18 backend, #20 frontend). Mirrors the backend
// CurrencyIcon enum; points stay "points" everywhere — the glyph is purely presentational.
export type CurrencyIcon =
  | "Star"
  | "Coin"
  | "Gem"
  | "Heart"
  | "Flower"
  | "JellyBean"
  | "Apple"
  | "Trophy"
  | "Crown"
  | "Rainbow";

// One lucide glyph per allowed value, so every screen (grid, leaderboard, shop, reports) renders the
// same icon for a class's currency. Keyed by the exact enum name the API returns.
export const currencyGlyphs: Record<CurrencyIcon, LucideIcon> = {
  Star,
  Coin: Coins,
  Gem,
  Heart,
  Flower: Flower2,
  JellyBean: Candy,
  Apple,
  Trophy,
  Crown,
  Rainbow,
};

// The fixed, ordered set offered in the settings picker (slice #26).
export const currencyIcons = Object.keys(currencyGlyphs) as CurrencyIcon[];

// Romanian labels for the picker; the glyphs themselves carry the meaning on the kid-facing screens.
export const currencyLabels: Record<CurrencyIcon, string> = {
  Star: "Steluță",
  Coin: "Monedă",
  Gem: "Nestemată",
  Heart: "Inimă",
  Flower: "Floare",
  JellyBean: "Jeleu",
  Apple: "Măr",
  Trophy: "Trofeu",
  Crown: "Coroană",
  Rainbow: "Curcubeu",
};

export function glyphFor(icon: CurrencyIcon): LucideIcon {
  return currencyGlyphs[icon] ?? Star;
}
