"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Loader2 } from "lucide-react";
import { useMemo } from "react";
import {
  type AvatarRarity,
  type AvatarSlot,
  type EquippedSlot,
  type StudentAvatar,
  equipItem,
  fetchStore,
  fetchStudentAvatar,
  purchaseItem,
} from "@/lib/api";
import { avatarDataUri } from "@/lib/avatar";
import type { CurrencyIcon } from "@/lib/currency";
import { ro } from "@/lib/strings";
import { Points } from "@/components/dashboard/Points";
import { OptionCard, type OptionState } from "./OptionCard";

// The avatar store for one student (issue #23): a live preview of their current avatar + wallet, and
// the catalog grouped by slot. State per option is merged from two sources — the avatar endpoint's
// owned/equipped sets and the store endpoint's buyable (catalog-minus-owned) list with affordability.

const SLOT_ORDER: AvatarSlot[] = ["Hair", "HairColor", "SkinColor", "Eyes", "Mouth"];
const STATE_RANK: Record<OptionState, number> = { equipped: 0, owned: 1, buy: 2, insufficient: 3 };

type ShopOption = {
  id: string;
  slot: AvatarSlot;
  optionValue: string;
  displayName: string;
  cost: number;
  rarity: AvatarRarity | null;
  state: OptionState;
};

export function StudentShop({
  classId,
  studentId,
  studentName,
  currencyIcon,
  onBack,
}: {
  classId: string;
  studentId: string;
  studentName: string;
  currencyIcon: CurrencyIcon;
  onBack: () => void;
}) {
  const queryClient = useQueryClient();
  const avatarKey = ["avatar", classId, studentId];
  const storeKey = ["store", classId, studentId];

  const avatar = useQuery({ queryKey: avatarKey, queryFn: () => fetchStudentAvatar(classId, studentId) });
  const store = useQuery({ queryKey: storeKey, queryFn: () => fetchStore(classId, studentId) });

  const equip = useMutation({
    mutationFn: (o: ShopOption) => equipItem(classId, studentId, o.slot, o.id),
    // Optimistic swap so the preview updates immediately (issue #23 acceptance), reconciled on settle.
    onMutate: async (o) => {
      await queryClient.cancelQueries({ queryKey: avatarKey });
      const prev = queryClient.getQueryData<StudentAvatar>(avatarKey);
      queryClient.setQueryData<StudentAvatar>(avatarKey, (old) =>
        old ? { ...old, equipped: replaceSlot(old.equipped, o) } : old,
      );
      return { prev };
    },
    onError: (_e, _o, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(avatarKey, ctx.prev);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: avatarKey }),
  });

  const buy = useMutation({
    mutationFn: (o: ShopOption) => purchaseItem(classId, studentId, o.id),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: avatarKey }),
        queryClient.invalidateQueries({ queryKey: storeKey }),
        // The purchase wrote a Purchase ledger row, lowering the wallet the dashboard reads.
        queryClient.invalidateQueries({ queryKey: ["leaderboard", classId] }),
        queryClient.invalidateQueries({ queryKey: ["transactions", classId] }),
      ]),
  });

  const grouped = useMemo(() => {
    if (!avatar.data || !store.data) return null;
    const equippedIds = new Set(avatar.data.equipped.map((e) => e.itemId));
    const owned: ShopOption[] = avatar.data.owned.map((i) => ({
      id: i.id,
      slot: i.slot,
      optionValue: i.optionValue,
      displayName: i.displayName,
      cost: i.cost,
      rarity: i.rarity,
      state: equippedIds.has(i.id) ? "equipped" : "owned",
    }));
    const buyable: ShopOption[] = store.data.items.map((i) => ({
      id: i.id,
      slot: i.slot,
      optionValue: i.optionValue,
      displayName: i.displayName,
      cost: i.cost,
      rarity: i.rarity,
      state: i.affordable ? "buy" : "insufficient",
    }));

    const bySlot = new Map<AvatarSlot, ShopOption[]>();
    for (const o of [...owned, ...buyable]) {
      const list = bySlot.get(o.slot) ?? [];
      list.push(o);
      bySlot.set(o.slot, list);
    }
    for (const list of bySlot.values()) {
      list.sort(
        (a, b) =>
          STATE_RANK[a.state] - STATE_RANK[b.state] ||
          a.cost - b.cost ||
          a.displayName.localeCompare(b.displayName),
      );
    }
    return bySlot;
  }, [avatar.data, store.data]);

  if (!avatar.data || !store.data || !grouped) {
    return (
      <div className="grid place-items-center py-24 text-indigo-900/60">
        <Loader2 className="animate-spin" size={28} />
      </div>
    );
  }

  const equipped = avatar.data.equipped;
  const pendingId = equip.isPending ? equip.variables?.id : buy.isPending ? buy.variables?.id : null;

  return (
    <div className="grid gap-6 lg:grid-cols-[260px_1fr]">
      {/* Live preview + wallet (sticky on wide screens so it stays visible while scrolling options). */}
      <aside className="lg:sticky lg:top-6 lg:self-start">
        <button
          type="button"
          onClick={onBack}
          className="mb-3 flex items-center gap-1.5 text-sm font-bold text-violet-600 transition hover:text-violet-800"
        >
          <ArrowLeft size={16} /> {ro.shop.back}
        </button>
        <div className="rounded-3xl border border-purple-100 bg-white p-5 text-center shadow-md shadow-purple-100/60">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={avatarDataUri(studentId, equipped)}
            alt=""
            width={160}
            height={160}
            className="mx-auto h-40 w-40"
          />
          <h2 className="mt-3 text-lg font-extrabold text-slate-800">{studentName}</h2>
          <p className="mt-1 flex items-center justify-center gap-1 text-sm text-slate-500">
            <Points value={store.data.wallet} icon={currencyIcon} size={14} tone="neutral" showSign={false} />
            {ro.shop.available}
          </p>
        </div>
      </aside>

      <div className="space-y-6">
        {SLOT_ORDER.map((slot) => {
          const options = grouped.get(slot);
          if (!options || options.length === 0) return null;
          return (
            <section key={slot}>
              <h3 className="mb-3 text-sm font-bold uppercase tracking-wide text-slate-400">
                {ro.shop.slots[slot]}
              </h3>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4">
                {options.map((o) => (
                  <OptionCard
                    key={o.id}
                    seed={studentId}
                    previewEquipped={replaceSlot(equipped, o)}
                    displayName={o.displayName}
                    rarity={o.rarity}
                    cost={o.cost}
                    state={o.state}
                    currencyIcon={currencyIcon}
                    pending={pendingId === o.id}
                    onEquip={() => equip.mutate(o)}
                    onBuy={() => buy.mutate(o)}
                  />
                ))}
              </div>
            </section>
          );
        })}

        {buy.isError && (
          <p className="text-center text-sm font-semibold text-rose-600">
            {(buy.error as Error).message}
          </p>
        )}
      </div>
    </div>
  );
}

// Return a copy of the equipped slots with `option`'s slot swapped to its value (added if missing), for
// both the optimistic equip update and each option's per-tile preview.
function replaceSlot(equipped: EquippedSlot[], option: ShopOption): EquippedSlot[] {
  const entry: EquippedSlot = { slot: option.slot, itemId: option.id, optionValue: option.optionValue };
  return equipped.some((e) => e.slot === option.slot)
    ? equipped.map((e) => (e.slot === option.slot ? entry : e))
    : [...equipped, entry];
}
