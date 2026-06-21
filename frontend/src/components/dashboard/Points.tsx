import { glyphFor, type CurrencyIcon } from "@/lib/currency";

// A signed point value rendered with the class currency glyph. Points stay "points" in code; the glyph
// is purely presentational (CLAUDE.md). `tone` lets a neutral context (e.g. the wallet on a card) opt
// out of the emerald/rose semantics that suit deltas.
export function Points({
  value,
  icon,
  size = 14,
  tone = "signed",
  showSign = true,
}: {
  value: number;
  icon: CurrencyIcon;
  size?: number;
  tone?: "signed" | "neutral";
  showSign?: boolean;
}) {
  const Glyph = glyphFor(icon);
  const color =
    tone === "neutral"
      ? "text-slate-600"
      : value > 0
        ? "text-emerald-600"
        : value < 0
          ? "text-rose-600"
          : "text-slate-400";
  const label = showSign && value > 0 ? `+${value}` : `${value}`;

  return (
    <span className={`inline-flex items-center gap-1 font-extrabold tabular-nums ${color}`}>
      <Glyph size={size} className="fill-amber-300 text-amber-400" />
      {label}
    </span>
  );
}
