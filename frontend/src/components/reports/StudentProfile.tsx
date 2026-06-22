"use client";

import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { ArrowLeft, Coins, History, Loader2, TrendingUp, Trophy } from "lucide-react";
import { useMemo } from "react";
import {
  type StudentHistoryItem,
  fetchStudentHistory,
  fetchStudentTimeline,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { formatDateTime, ymd } from "@/lib/dates";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { StudentAvatar } from "@/components/dashboard/StudentAvatar";
import { TimelineChart } from "./TimelineChart";

// A student's report profile (slice #25): three stat cards (lifetime earned, spendable wallet,
// this-month net), a point-timeline chart for the current month, and the full paginated event history
// with notes and dates. Wallet/lifetime come from the parent's leaderboard row; the chart and stat
// reuse the timeline endpoint, and the history is its own paginated endpoint (the class feed is capped).
export function StudentProfile({
  classId,
  studentId,
  studentName,
  wallet,
  lifetimeEarned,
  currencyIcon,
  onBack,
}: {
  classId: string;
  studentId: string;
  studentName: string;
  wallet: number;
  lifetimeEarned: number;
  currencyIcon: CurrencyIcon;
  onBack: () => void;
}) {
  // The current calendar month, local: from the 1st through today. The chart and "this month" stat
  // share this window so the subtitle reads honestly as "this month".
  const { from, to } = useMemo(() => {
    const now = new Date();
    const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
    return { from: ymd(firstOfMonth), to: ymd(now) };
  }, []);

  const timeline = useQuery({
    queryKey: ["timeline", classId, studentId, from, to],
    queryFn: () => fetchStudentTimeline(classId, studentId, from, to),
  });

  const history = useInfiniteQuery({
    queryKey: ["studentHistory", classId, studentId],
    queryFn: ({ pageParam }) => fetchStudentHistory(classId, studentId, pageParam),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
  });

  // Net this month = earned + spent (spent is non-positive), straight off the timeline totals.
  const thisMonthNet = timeline.data ? timeline.data.totalEarned + timeline.data.totalSpent : 0;

  const historyItems = history.data?.pages.flatMap((p) => p.items) ?? [];
  const totalCount = history.data?.pages[0]?.totalCount ?? 0;

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={onBack}
        className="flex items-center gap-1.5 text-sm font-bold text-violet-600 transition hover:text-violet-800"
      >
        <ArrowLeft size={16} /> {ro.reports.back}
      </button>

      <header className="flex items-center gap-4">
        <StudentAvatar classId={classId} studentId={studentId} size={64} />
        <h2 className="text-2xl font-extrabold tracking-tight text-slate-800">{studentName}</h2>
      </header>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <StatCard
          icon={Trophy}
          label={ro.reports.profile.lifetime}
          hint={ro.reports.profile.lifetimeHint}
          value={lifetimeEarned}
          currencyIcon={currencyIcon}
          tone="neutral"
        />
        <StatCard
          icon={Coins}
          label={ro.reports.profile.spendable}
          hint={ro.reports.profile.spendableHint}
          value={wallet}
          currencyIcon={currencyIcon}
          tone="neutral"
        />
        <StatCard
          icon={TrendingUp}
          label={ro.reports.profile.thisMonth}
          hint={ro.reports.profile.thisMonthHint}
          value={thisMonthNet}
          currencyIcon={currencyIcon}
          tone="signed"
        />
      </div>

      <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
        <header className="mb-4 flex items-center gap-2">
          <TrendingUp size={20} className="text-violet-500" />
          <h3 className="flex-1 font-extrabold text-slate-800">{ro.reports.profile.timelineTitle}</h3>
          <span className="text-xs font-semibold text-slate-400">
            {ro.reports.profile.timelineSubtitle}
          </span>
        </header>
        {timeline.isLoading ? (
          <div className="grid place-items-center py-12 text-indigo-900/50">
            <Loader2 className="animate-spin" size={24} />
          </div>
        ) : timeline.data ? (
          <TimelineChart days={timeline.data.days} />
        ) : null}
      </section>

      <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
        <header className="mb-4 flex items-center gap-2">
          <History size={20} className="text-violet-500" />
          <h3 className="flex-1 font-extrabold text-slate-800">{ro.reports.profile.historyTitle}</h3>
          {totalCount > 0 && (
            <span className="text-xs font-semibold text-slate-400">
              {ro.reports.profile.historyCount(historyItems.length, totalCount)}
            </span>
          )}
        </header>

        {history.isLoading ? (
          <div className="grid place-items-center py-12 text-indigo-900/50">
            <Loader2 className="animate-spin" size={24} />
          </div>
        ) : historyItems.length === 0 ? (
          <p className="py-8 text-center text-sm text-slate-400">{ro.reports.profile.historyEmpty}</p>
        ) : (
          <>
            <ul className="space-y-1">
              {historyItems.map((item) => (
                <HistoryRow key={item.id} item={item} currencyIcon={currencyIcon} />
              ))}
            </ul>
            {history.hasNextPage && (
              <button
                type="button"
                onClick={() => history.fetchNextPage()}
                disabled={history.isFetchingNextPage}
                className="mt-4 w-full rounded-2xl bg-violet-100 py-2.5 text-sm font-bold text-violet-700 transition hover:bg-violet-200 disabled:opacity-50"
              >
                {history.isFetchingNextPage
                  ? ro.reports.profile.loadingMore
                  : ro.reports.profile.loadMore}
              </button>
            )}
          </>
        )}
      </section>
    </div>
  );
}

function StatCard({
  icon: Icon,
  label,
  hint,
  value,
  currencyIcon,
  tone,
}: {
  icon: typeof Trophy;
  label: string;
  hint: string;
  value: number;
  currencyIcon: CurrencyIcon;
  tone: "neutral" | "signed";
}) {
  return (
    <div className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
      <div className="flex items-center gap-2 text-violet-500">
        <Icon size={18} />
        <p className="text-xs font-bold uppercase tracking-wide text-slate-400">{label}</p>
      </div>
      <div className="mt-2 text-2xl">
        <Points value={value} icon={currencyIcon} size={22} tone={tone} showSign={tone === "signed"} />
      </div>
      <p className="mt-0.5 text-xs text-slate-400">{hint}</p>
    </div>
  );
}

// Behavior name / purchase / note / type fallback — mirrors the dashboard's recent-activity labelling.
function describe(item: StudentHistoryItem): string {
  if (item.type === "Purchase") return ro.reports.profile.types.Purchase;
  if (item.behaviorName) return item.behaviorName;
  if (item.reason) return item.reason;
  return ro.reports.profile.types[item.type];
}

function HistoryRow({
  item,
  currencyIcon,
}: {
  item: StudentHistoryItem;
  currencyIcon: CurrencyIcon;
}) {
  const voided = item.voidedAt != null;
  // The behavior is the headline when present, so the note becomes useful secondary context.
  const note = item.behaviorName && item.reason ? item.reason : null;

  return (
    <li className="flex items-center gap-3 rounded-xl px-2 py-2 hover:bg-slate-50">
      <div className="min-w-0 flex-1">
        <p
          className={`truncate text-sm font-semibold ${
            voided ? "text-slate-400 line-through" : "text-slate-700"
          }`}
        >
          {describe(item)}
        </p>
        <p className="truncate text-xs text-slate-400">
          {formatDateTime(item.createdAt)}
          {note ? ` · ${note}` : ""}
          {voided ? ` · ${ro.reports.profile.voided}` : ""}
        </p>
      </div>
      <Points value={item.amount} icon={currencyIcon} size={12} />
    </li>
  );
}
