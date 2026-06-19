# Slice 26 — Kiosk redesign

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A locked-down, projector/tablet-friendly kiosk view for one class, in the new visual language over the existing reduced-scope kiosk session.

- A large grid of student avatars. Tapping one opens that student's personal area: their stars (with the class currency icon), their avatar, and the store to browse / buy / equip.
- An understated, read-only leaderboard is visible.
- No teacher or admin controls are reachable (the server already rejects them for the kiosk principal).
- Exiting returns to the full teacher session via the PIN.

Designed for large touch targets and readability on a projector or tablet.

## Acceptance criteria

- [ ] Kiosk shows a large student-avatar grid; tapping a student opens their stars + avatar + shop.
- [ ] A student can browse, buy, and equip avatar options in kiosk; teacher/admin actions are not reachable.
- [ ] A read-only leaderboard is visible but understated.
- [ ] Exiting kiosk requires the correct PIN and restores the teacher session.
- [ ] The layout uses large targets and is readable on a projector / tablet.

## Blocked by

- #22 — Dashboard
- #23 — Magazin redesign

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
