# Slice 7 — Avatar foundation: DiceBear render + free defaults + equip

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Every student has an avatar rendered client-side from a single **locked DiceBear style**; new students automatically own the free default options; a student's avatar shows in the roster, and owned options can be equipped per slot for free.

Scope:
- Lock one DiceBear style; `AvatarItem` catalog (Slot, OptionValue, Cost, Rarity?, IsDefault, DisplayName), seeded by enumerating that style's options.
- `StudentOwnedItem` (new students own `IsDefault` items); `StudentEquipped` (PK StudentId, Slot).
- Frontend composes the equipped options via DiceBear.

## Acceptance criteria

- [x] Catalog is seeded from the chosen style's options, with free default items flagged.
- [x] New students own the default items and render a valid avatar.
- [x] A student can equip any owned option per slot (free) and switch freely.
- [x] The avatar renders in the roster from the equipped configuration.
- [x] Authorization enforced.

## Blocked by

- #4 — Roster

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
