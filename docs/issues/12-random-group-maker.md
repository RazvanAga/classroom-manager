# Slice 11 — Random group maker (size, even, gender-balanced) + optional persist

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A teacher forms random groups by choosing students-per-group; group sizes are even (differ by at most one); optionally balanced by gender so Female/Male (and unrecorded) spread evenly across groups; a formed grouping can be saved to reuse during the activity.

Scope:
- **Pure, seedable grouping function**: group-size input; even distribution; optional gender balance via stratified round-robin over `{Female, Male, null}` buckets. Unit-tested.
- Optional `Grouping` + `GroupMember` persistence.
- UI.

## Acceptance criteria

- [x] Choosing a group size yields evenly-sized groups (differ by ≤1) — unit-tested incl. edge cases (exact division, remainder, fewer students than group size).
- [x] The gender-balanced option spreads each bucket (incl. unrecorded) evenly across groups — unit-tested.
- [x] Grouping is deterministic given a fixed seed.
- [x] A formed grouping can be saved and retrieved.

## Blocked by

- #4 — Roster

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
