"use client";

import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createAvatar } from "@dicebear/core";
import { adventurer } from "@dicebear/collection";
import {
  equipItem,
  fetchStore,
  fetchStudentAvatar,
  purchaseItem,
  type AvatarSlot,
  type EquippedSlot,
} from "@/lib/api";

// The slot order shown in the customizer (matches the always-on slots the backend models).
export const SLOT_ORDER: AvatarSlot[] = ["Hair", "HairColor", "SkinColor", "Eyes", "Mouth"];
export const SLOT_LABELS: Record<AvatarSlot, string> = {
  Hair: "Hair",
  HairColor: "Hair color",
  SkinColor: "Skin",
  Eyes: "Eyes",
  Mouth: "Mouth",
};

// Compose the DiceBear SVG from the equipped options. The backend stores no render metadata
// (design.md §3.2) — the frontend owns composition entirely. The enum slot maps to the DiceBear
// option key by lower-casing the first letter (HairColor → hairColor).
export function avatarUri(seed: string, equipped: EquippedSlot[], size: number): string {
  const options: Record<string, string[]> = {};
  for (const e of equipped) {
    const key = e.slot.charAt(0).toLowerCase() + e.slot.slice(1);
    options[key] = [e.optionValue];
  }
  return createAvatar(adventurer, { seed, size, ...options }).toDataUri();
}

// The avatar customizer + store for one student: composed preview, owned options per slot (equip is
// free), and the buyable catalog with affordability. Shared by the teacher roster row and the kiosk
// view — the endpoints it calls accept a member-teacher OR a kiosk principal scoped to the class.
export function StudentShop({ classId, studentId }: { classId: string; studentId: string }) {
  const queryClient = useQueryClient();
  const avatarKey = ["avatar", classId, studentId];
  const storeKey = ["store", classId, studentId];

  const { data: avatar } = useQuery({
    queryKey: avatarKey,
    queryFn: () => fetchStudentAvatar(classId, studentId),
  });

  const { data: store } = useQuery({
    queryKey: storeKey,
    queryFn: () => fetchStore(classId, studentId),
  });

  const equip = useMutation({
    mutationFn: (vars: { slot: AvatarSlot; itemId: string }) =>
      equipItem(classId, studentId, vars.slot, vars.itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: avatarKey }),
  });

  // Buying moves wallet points (a ledger row) and adds to inventory, so refresh both the store and
  // the avatar (the new option shows up as an equippable owned item).
  const buy = useMutation({
    mutationFn: (itemId: string) => purchaseItem(classId, studentId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: storeKey });
      queryClient.invalidateQueries({ queryKey: avatarKey });
    },
  });

  const uri = useMemo(
    () => (avatar ? avatarUri(studentId, avatar.equipped, 96) : null),
    [avatar, studentId],
  );

  // Currently-equipped item per slot, for highlighting the active option.
  const equippedBySlot = useMemo(() => {
    const map = new Map<AvatarSlot, string>();
    for (const e of avatar?.equipped ?? []) map.set(e.slot, e.itemId);
    return map;
  }, [avatar]);

  if (!avatar) return null;

  return (
    <div className="avatar-editor">
      <img className="avatar-preview" src={uri ?? undefined} width={96} height={96} alt="" />
      <div className="avatar-slots">
        {SLOT_ORDER.map((slot) => {
          const options = avatar.owned.filter((o) => o.slot === slot);
          if (options.length === 0) return null;
          const activeId = equippedBySlot.get(slot);
          return (
            <div key={slot} className="avatar-slot">
              <span className="avatar-slot-label">{SLOT_LABELS[slot]}</span>
              <div className="behavior-chips">
                {options.map((opt) => (
                  <button
                    key={opt.id}
                    className={`chip avatar-option${opt.id === activeId ? " selected" : ""}`}
                    disabled={equip.isPending || opt.id === activeId}
                    onClick={() => equip.mutate({ slot, itemId: opt.id })}
                  >
                    {opt.displayName}
                  </button>
                ))}
              </div>
            </div>
          );
        })}
      </div>
      {equip.isError && <p className="error">{(equip.error as Error).message}</p>}

      <div className="avatar-store">
        <div className="avatar-store-head">
          <span className="avatar-slot-label">Store</span>
          {store && <span className="wallet-tag">{store.wallet} pts</span>}
        </div>
        {store && store.items.length === 0 && (
          <p className="muted">Everything's been bought — nice collection!</p>
        )}
        {store && store.items.length > 0 && (
          <div className="behavior-chips">
            {store.items.map((item) => (
              <button
                key={item.id}
                className="chip avatar-option buy"
                disabled={buy.isPending || !item.affordable}
                title={
                  item.affordable
                    ? `Buy ${item.displayName} for ${item.cost} pts`
                    : `Costs ${item.cost} pts — not enough points yet`
                }
                onClick={() => buy.mutate(item.id)}
              >
                {SLOT_LABELS[item.slot]}: {item.displayName} · {item.cost}
              </button>
            ))}
          </div>
        )}
        {buy.isError && <p className="error">{(buy.error as Error).message}</p>}
      </div>
    </div>
  );
}
