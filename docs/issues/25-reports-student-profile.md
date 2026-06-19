# Slice 24 — Rapoarte + student profile

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Read-only analytics, in two views, backed by one small new endpoint.

- **New endpoint:** a per-student transaction history — paginated and including each entry's note/reason — so a teacher can see *why* points were given (the class-wide feed is capped and not per-student).
- **Student profile page:** three stat cards (lifetime total, current spendable, this month), a point-timeline chart, and the full event history with notes and dates (Romanian formatting).
- **Class reports page:** the per-class behavior breakdown plus a student picker that opens that student's timeline.

All aggregations reuse the existing reporting logic and exclude voided rows.

## Acceptance criteria

- [ ] A paginated per-student transaction history (including notes) is available and authorized (member-only, cross-tenant denied).
- [ ] The student profile shows lifetime / spendable / this-month stats, a timeline chart, and the event history with notes.
- [ ] The reports page shows the per-class behavior breakdown and a per-student timeline via a picker.
- [ ] All aggregations exclude voided rows.

## Blocked by

- #21 — App shell + class selector

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
