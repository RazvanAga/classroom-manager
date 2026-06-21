"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { History, RotateCcw, Users } from "lucide-react";
import {
  type PointTransaction,
  voidBatch,
  voidTransaction,
} from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "./Points";

// One feed row: either a standalone transaction or a bulk award collapsed by its batch id, so a batch
// undoes as a single unit (design.md §2.4). The list arrives most-recent-first, and a batch's rows are
// written together, so first-seen order is preserved by keying into an index map.
type FeedItem =
  | { kind: "single"; tx: PointTransaction }
  | { kind: "batch"; batchId: string; rows: PointTransaction[] };

function groupByBatch(transactions: PointTransaction[]): FeedItem[] {
  const items: FeedItem[] = [];
  const batchIndex = new Map<string, number>();
  for (const tx of transactions) {
    if (tx.batchId) {
      const existing = batchIndex.get(tx.batchId);
      if (existing !== undefined) {
        (items[existing] as { rows: PointTransaction[] }).rows.push(tx);
      } else {
        batchIndex.set(tx.batchId, items.length);
        items.push({ kind: "batch", batchId: tx.batchId, rows: [tx] });
      }
    } else {
      items.push({ kind: "single", tx });
    }
  }
  return items;
}

// Behavior/purchase/adjustment label for a standalone row.
function describe(tx: PointTransaction): string {
  if (tx.type === "Purchase") return ro.recent.purchase;
  if (tx.behaviorName) return tx.behaviorName;
  if (tx.reason) return tx.reason;
  return ro.recent.adjustment;
}

export function RecentActivity({
  classId,
  transactions,
  currencyIcon,
}: {
  classId: string;
  transactions: PointTransaction[];
  currencyIcon: CurrencyIcon;
}) {
  const queryClient = useQueryClient();
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["leaderboard", classId] }),
      queryClient.invalidateQueries({ queryKey: ["transactions", classId] }),
    ]);

  const undoOne = useMutation({
    mutationFn: (transactionId: string) => voidTransaction(classId, transactionId),
    onSuccess: invalidate,
  });
  const undoBatch = useMutation({
    mutationFn: (batchId: string) => voidBatch(classId, batchId),
    onSuccess: invalidate,
  });

  const items = groupByBatch(transactions);

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-5 shadow-md shadow-purple-100/60">
      <header className="mb-4 flex items-center gap-2">
        <History size={20} className="text-violet-500" />
        <h2 className="font-extrabold text-slate-800">{ro.recent.title}</h2>
      </header>

      {items.length === 0 ? (
        <p className="py-8 text-center text-sm text-slate-400">{ro.recent.empty}</p>
      ) : (
        <ul className="space-y-1">
          {items.map((item) => {
            if (item.kind === "batch") {
              const first = item.rows[0];
              const voided = item.rows.every((r) => r.voidedAt);
              const label = first.behaviorName ?? first.reason ?? ro.recent.adjustment;
              return (
                <Row
                  key={item.batchId}
                  title={label}
                  meta={
                    <span className="flex items-center gap-1">
                      <Users size={11} /> {ro.recent.batch(item.rows.length)}
                    </span>
                  }
                  amount={first.amount}
                  currencyIcon={currencyIcon}
                  voided={voided}
                  pending={undoBatch.isPending}
                  onUndo={() => undoBatch.mutate(item.batchId)}
                />
              );
            }
            const { tx } = item;
            const canUndo = tx.type !== "Purchase";
            return (
              <Row
                key={tx.id}
                title={tx.studentName}
                meta={describe(tx)}
                amount={tx.amount}
                currencyIcon={currencyIcon}
                voided={tx.voidedAt != null}
                pending={undoOne.isPending}
                onUndo={canUndo ? () => undoOne.mutate(tx.id) : undefined}
              />
            );
          })}
        </ul>
      )}
    </section>
  );
}

function Row({
  title,
  meta,
  amount,
  currencyIcon,
  voided,
  pending,
  onUndo,
}: {
  title: string;
  meta: React.ReactNode;
  amount: number;
  currencyIcon: CurrencyIcon;
  voided: boolean;
  pending: boolean;
  onUndo?: () => void;
}) {
  return (
    <li className="flex items-center gap-3 rounded-xl px-2 py-2 hover:bg-slate-50">
      <div className="min-w-0 flex-1">
        <p
          className={`truncate text-sm font-semibold ${
            voided ? "text-slate-400 line-through" : "text-slate-700"
          }`}
        >
          {title}
        </p>
        <p className="truncate text-xs text-slate-400">{meta}</p>
      </div>
      <Points value={amount} icon={currencyIcon} size={12} />
      {voided ? (
        <span className="w-16 text-right text-xs font-semibold text-slate-300">
          {ro.recent.undone}
        </span>
      ) : onUndo ? (
        <button
          type="button"
          onClick={onUndo}
          disabled={pending}
          className="flex w-16 items-center justify-end gap-1 text-xs font-bold text-violet-500 transition hover:text-violet-700 disabled:opacity-50"
        >
          <RotateCcw size={13} /> {ro.recent.undo}
        </button>
      ) : (
        <span className="w-16" />
      )}
    </li>
  );
}
