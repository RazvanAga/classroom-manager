import { createAvatar } from "@dicebear/core";
import { adventurer } from "@dicebear/collection";
import type { AvatarSlot, EquippedSlot } from "./api";

// The locked DiceBear style is composed entirely on the frontend (CLAUDE.md invariant: the backend
// can't run DiceBear, so it stores only option values). Each equipped slot pins one schema option;
// the `seed` keeps every layer the catalog doesn't cover (eyebrows, features…) stable per student.

const slotToOption: Record<AvatarSlot, "hair" | "hairColor" | "skinColor" | "eyes" | "mouth"> = {
  Hair: "hair",
  HairColor: "hairColor",
  SkinColor: "skinColor",
  Eyes: "eyes",
  Mouth: "mouth",
};

// Build a data-URI SVG for a student's avatar from their equipped slots. Memoizable by caller via the
// avatar query cache; computing one is cheap and synchronous (no network).
export function avatarDataUri(seed: string, equipped: EquippedSlot[]): string {
  const options: Record<string, string[]> = {};
  for (const slot of equipped) {
    options[slotToOption[slot.slot]] = [slot.optionValue];
  }
  return createAvatar(adventurer, {
    seed,
    radius: 50,
    backgroundColor: ["ede9fe"],
    ...options,
  }).toDataUri();
}
