import { Check } from "lucide-react";
import type { Student } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { Points } from "./Points";
import { StudentAvatar } from "./StudentAvatar";

// One tappable student tile: rank badge, avatar, first name, spendable wallet. In multi-select mode it
// shows a checkable state instead of opening the award sheet. Gender lightly tints the card (the
// prototype's pink/sky), falling back to violet when gender is unset.
export function StudentCard({
  classId,
  student,
  wallet,
  rank,
  currencyIcon,
  selectable,
  selected,
  onClick,
}: {
  classId: string;
  student: Student;
  wallet: number;
  rank: number;
  currencyIcon: CurrencyIcon;
  selectable: boolean;
  selected: boolean;
  onClick: () => void;
}) {
  const tint =
    student.gender === "Female"
      ? "border-pink-200 bg-pink-50 hover:border-pink-400"
      : student.gender === "Male"
        ? "border-sky-200 bg-sky-50 hover:border-sky-400"
        : "border-violet-200 bg-violet-50 hover:border-violet-400";

  const firstName = student.displayName.split(" ")[0];

  return (
    <button
      type="button"
      onClick={onClick}
      className={`group relative flex cursor-pointer flex-col items-center gap-1.5 rounded-2xl border-2 p-2 pt-3 transition-all duration-150 hover:scale-105 hover:shadow-lg ${
        selected ? "border-violet-500 bg-violet-100 ring-2 ring-violet-300" : tint
      }`}
    >
      <span className="absolute left-2 top-1.5 text-[10px] font-bold text-slate-400">#{rank}</span>

      {selectable && (
        <span
          className={`absolute right-1.5 top-1.5 flex h-5 w-5 items-center justify-center rounded-full border-2 transition ${
            selected
              ? "border-violet-500 bg-violet-500 text-white"
              : "border-slate-300 bg-white text-transparent"
          }`}
        >
          <Check size={12} strokeWidth={3} />
        </span>
      )}

      <StudentAvatar classId={classId} studentId={student.id} size={56} />

      <span className="line-clamp-2 px-0.5 text-center text-xs font-bold leading-tight text-slate-700">
        {firstName}
      </span>

      <Points value={wallet} icon={currencyIcon} size={11} tone="neutral" showSign={false} />
    </button>
  );
}
