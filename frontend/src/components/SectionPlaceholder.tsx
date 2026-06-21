import { type LucideIcon } from "lucide-react";
import { ro } from "@/lib/strings";

// Temporary body for a section whose full build lands in a later slice (#22–#26). Keeps the shell
// navigable and demoable now without pretending the feature exists yet.
export function SectionPlaceholder({ title, icon: Icon }: { title: string; icon: LucideIcon }) {
  return (
    <div className="grid place-items-center rounded-3xl border border-dashed border-violet-200 bg-white/60 px-6 py-20 text-center">
      <Icon size={40} className="text-violet-300" />
      <h2 className="mt-4 text-2xl font-extrabold tracking-tight">{title}</h2>
      <p className="mt-1 text-slate-400">{ro.sections.placeholder(title)}</p>
    </div>
  );
}
