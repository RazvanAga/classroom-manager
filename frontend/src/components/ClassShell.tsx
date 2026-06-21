"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  BarChart3,
  LayoutDashboard,
  type LucideIcon,
  MonitorPlay,
  Settings,
  Shuffle,
  ShoppingBag,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect } from "react";
import { enterKiosk, fetchClasses } from "@/lib/api";
import { glyphFor } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Loader } from "./Loader";

type Section = { segment: string; label: string; icon: LucideIcon };

// The five persistent per-class sections. Later slices fill each page; the nav is established here.
const sections: Section[] = [
  { segment: "/dashboard", label: ro.nav.dashboard, icon: LayoutDashboard },
  { segment: "/magazin", label: ro.nav.shop, icon: ShoppingBag },
  { segment: "/grupuri", label: ro.nav.groups, icon: Shuffle },
  { segment: "/rapoarte", label: ro.nav.reports, icon: BarChart3 },
  { segment: "/setari", label: ro.nav.settings, icon: Settings },
];

// The class-scoped shell: a persistent top nav + a class header (name, currency icon, Kiosk entry).
// The class is selected via the `?class=` URL param (search-param scoping keeps the static export
// building without server rewrites — no dynamic route segments to pre-render). An unknown/missing
// class falls back to the selector.
export function ClassShell({ children }: { children: React.ReactNode }) {
  const params = useSearchParams();
  const pathname = usePathname();
  const router = useRouter();
  const queryClient = useQueryClient();
  const classId = params.get("class");

  const { data: classes, isLoading } = useQuery({ queryKey: ["classes"], queryFn: fetchClasses });
  const klass = classes?.find((c) => c.id === classId) ?? null;

  const enterKioskMutation = useMutation({
    mutationFn: () => enterKiosk(classId!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["kioskSession"] }),
  });

  useEffect(() => {
    if (!isLoading && !klass) router.replace("/");
  }, [isLoading, klass, router]);

  if (isLoading || !klass) return <Loader />;

  const Glyph = glyphFor(klass.currencyIcon);
  const withClass = (segment: string) => `${segment}?class=${klass.id}`;

  return (
    <div className="flex min-h-screen flex-col">
      <nav className="bg-gradient-to-r from-violet-700 via-purple-600 to-indigo-600 shadow-lg shadow-purple-900/30">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-3 px-4">
          <Link
            href="/"
            className="flex items-center gap-2 rounded-xl px-3 py-2 text-sm font-semibold text-purple-100 transition hover:bg-white/10 hover:text-white"
          >
            <ArrowLeft size={18} />
            <span className="hidden sm:inline">{ro.nav.backToClasses}</span>
          </Link>

          <div className="flex gap-1">
            {sections.map(({ segment, label, icon: Icon }) => {
              const active = pathname === segment;
              return (
                <Link
                  key={segment}
                  href={withClass(segment)}
                  className={`flex items-center gap-2 rounded-xl px-3 py-2 text-sm font-semibold transition ${
                    active
                      ? "bg-white/20 text-white shadow-inner"
                      : "text-purple-100 hover:bg-white/10 hover:text-white"
                  }`}
                >
                  <Icon size={17} />
                  <span className="hidden md:inline">{label}</span>
                </Link>
              );
            })}
          </div>
        </div>
      </nav>

      <header className="border-b border-violet-100 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 rotate-3 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-500 text-white shadow-md">
              <Glyph size={22} className="fill-yellow-300 text-yellow-300" />
            </div>
            <h1 className="text-2xl font-extrabold tracking-tight">{klass.name}</h1>
          </div>
          <button
            type="button"
            onClick={() => enterKioskMutation.mutate()}
            disabled={enterKioskMutation.isPending}
            className="flex items-center gap-2 rounded-xl border border-violet-200 bg-violet-50 px-4 py-2 text-sm font-bold text-violet-700 transition hover:bg-violet-100 disabled:opacity-50"
          >
            <MonitorPlay size={18} />
            {enterKioskMutation.isPending ? ro.nav.enteringKiosk : ro.nav.kiosk}
          </button>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</main>
    </div>
  );
}
