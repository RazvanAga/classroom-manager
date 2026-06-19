# Slice 22 — Magazin redesign

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

The avatar store, rebuilt in the new visual language over the existing store/purchase/equip endpoints.

- Select a student (a grid with each student's wallet) to open their shop.
- Show a live DiceBear preview of the student's current avatar plus their wallet (currency icon).
- Buyable options are grouped by avatar slot (Hair, Hair color, Skin, Eyes, Mouth). Each option shows one of four states: Equipped, Owned (Equip), Buy, or Insufficient.
- Rarity is used as a professional visual accent — Common → Legendary — with Legendary given a distinct frame.
- Buying runs the existing transactional purchase (no overspend, no double-buy). Equipping an owned option is free and updates the live preview immediately.

The DiceBear style stays locked; we recover the "fun" through polish (rarity framing, live preview), not new avatar data.

## Acceptance criteria

- [ ] Selecting a student opens their shop with a live DiceBear preview and wallet (currency icon).
- [ ] Options are grouped by slot and show the correct state (equipped / owned / affordable / insufficient).
- [ ] Buying an affordable option succeeds and is reflected; an unaffordable option is blocked; double-buy is blocked.
- [ ] Equipping an owned option updates the preview immediately.
- [ ] Rarity is visible as an accent and Legendary is visually distinct.

## Blocked by

- #21 — App shell + class selector

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
