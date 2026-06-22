import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

// The shared section shell for the settings page: a rounded card with an icon, title, optional
// count badge and subtitle. Keeps every section visually consistent (issue #26).
export function SettingsCard({
  icon: Icon,
  title,
  subtitle,
  badge,
  tone = "default",
  children,
}: {
  icon: LucideIcon;
  title: string;
  subtitle?: string;
  badge?: ReactNode;
  tone?: "default" | "danger";
  children: ReactNode;
}) {
  const accent = tone === "danger" ? "text-rose-500" : "text-violet-500";
  const border = tone === "danger" ? "border-rose-100" : "border-purple-100";

  return (
    <section className={`rounded-3xl border ${border} bg-white p-6 shadow-sm`}>
      <div className="mb-1 flex items-center gap-2">
        <Icon size={20} className={accent} />
        <h2 className="text-lg font-extrabold text-slate-800">{title}</h2>
        {badge != null && (
          <span className="ml-1 rounded-full bg-violet-100 px-2.5 py-0.5 text-sm font-bold text-violet-700">
            {badge}
          </span>
        )}
      </div>
      {subtitle && <p className="mb-5 text-sm text-slate-500">{subtitle}</p>}
      {!subtitle && <div className="mb-5" />}
      {children}
    </section>
  );
}
