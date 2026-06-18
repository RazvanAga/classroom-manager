"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createAvatar } from "@dicebear/core";
import { adventurer } from "@dicebear/collection";
import {
  equipItem,
  fetchStudentAvatar,
  type AvatarSlot,
  type EquippedSlot,
  type Student,
} from "@/lib/api";

// The slot order shown in the customizer (matches the always-on slots the backend models).
const SLOT_ORDER: AvatarSlot[] = ["Hair", "HairColor", "SkinColor", "Eyes", "Mouth"];
const SLOT_LABELS: Record<AvatarSlot, string> = {
  Hair: "Hair",
  HairColor: "Hair color",
  SkinColor: "Skin",
  Eyes: "Eyes",
  Mouth: "Mouth",
};

// Compose the DiceBear SVG from the equipped options. The backend stores no render metadata
// (design.md §3.2) — the frontend owns composition entirely. The enum slot maps to the DiceBear
// option key by lower-casing the first letter (HairColor → hairColor).
function avatarUri(seed: string, equipped: EquippedSlot[], size: number): string {
  const options: Record<string, string[]> = {};
  for (const e of equipped) {
    const key = e.slot.charAt(0).toLowerCase() + e.slot.slice(1);
    options[key] = [e.optionValue];
  }
  return createAvatar(adventurer, { seed, size, ...options }).toDataUri();
}

// One roster row: the student's composed avatar, name, a customize toggle, and remove. The avatar
// and its owned options are fetched per student (small classes, so N light queries are fine).
export function RosterStudent({
  classId,
  student,
  onRemove,
  removing,
}: {
  classId: string;
  student: Student;
  onRemove: () => void;
  removing: boolean;
}) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);

  const avatarKey = ["avatar", classId, student.id];
  const { data: avatar } = useQuery({
    queryKey: avatarKey,
    queryFn: () => fetchStudentAvatar(classId, student.id),
  });

  const equip = useMutation({
    mutationFn: (vars: { slot: AvatarSlot; itemId: string }) =>
      equipItem(classId, student.id, vars.slot, vars.itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: avatarKey }),
  });

  const uri = useMemo(
    () => (avatar ? avatarUri(student.id, avatar.equipped, 96) : null),
    [avatar, student.id],
  );

  // Currently-equipped item per slot, for highlighting the active option.
  const equippedBySlot = useMemo(() => {
    const map = new Map<AvatarSlot, string>();
    for (const e of avatar?.equipped ?? []) map.set(e.slot, e.itemId);
    return map;
  }, [avatar]);

  return (
    <li className="class-item roster-student">
      <div className="roster-student-head">
        {uri ? (
          <img className="avatar-thumb" src={uri} width={48} height={48} alt="" />
        ) : (
          <span className="avatar-thumb placeholder" aria-hidden />
        )}
        <span className="class-name">{student.displayName}</span>
        <span className="roster-right">
          {student.gender && (
            <span className="role-tag">{student.gender === "Female" ? "F" : "M"}</span>
          )}
          <button
            className="btn-ghost"
            aria-expanded={open}
            onClick={() => setOpen((v) => !v)}
            disabled={!avatar}
          >
            {open ? "Done" : "Customize"}
          </button>
          <button
            className="btn-ghost danger"
            aria-label={`Remove ${student.displayName}`}
            onClick={onRemove}
            disabled={removing}
          >
            Remove
          </button>
        </span>
      </div>

      {open && avatar && (
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
        </div>
      )}
    </li>
  );
}
