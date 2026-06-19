# Slice 18 — Per-class currency icon

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Each class chooses its own reward-currency icon (e.g. star, coin, gem, jelly bean, flower) from a fixed, curated set, defaulting to a star. This is the backend foundation: a currency-icon value on the class, validated against the allowed set, surfaced in class responses and changeable by a class member.

Points stay called "points" in the code and ledger — the icon is purely presentational. Children will later see only the icon + number, never a word. The fixed allowed set keeps rendering consistent across every screen (grid, leaderboard, shop, reports).

## Acceptance criteria

- [ ] A class has a currency icon, defaulting to `star` on creation and on seed.
- [ ] The icon is readable in class responses and can be changed to any value in the fixed allowed set.
- [ ] An invalid icon value is rejected (validation / 400).
- [ ] Authorization enforced — only class members can change it; cross-tenant change is denied.
- [ ] Integration tests cover the default, a valid change, an invalid rejection, and authz.

## Blocked by

- None - can start immediately.

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
