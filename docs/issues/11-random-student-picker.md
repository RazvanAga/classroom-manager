# Slice 10 — Random student picker (fair)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A teacher picks a random student fairly — no one is repeated until everyone has been picked once, then the cycle resets; the cycle can be reset on demand.

Scope:
- **Pure, seedable fair-pick function**: `(eligibleStudents, alreadyPickedIds, seed) → nextStudent`. Unit-tested.
- The already-picked set is held per session (no DB table); reset clears it.
- Picker UI.

## Acceptance criteria

- [x] The picker never repeats a student until all have been picked, then resets — unit-tested.
- [x] A pick is deterministic given a fixed seed (testable).
- [x] The teacher can reset the cycle on demand.
- [x] The picker operates over a class roster.

## Blocked by

- #4 — Roster

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
