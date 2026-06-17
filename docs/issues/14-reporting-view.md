# Slice 13 — Reporting view

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A lightweight analytics view: a per-student point timeline and a per-class behavior breakdown (most common positive/negative, totals) over a selectable date range.

Scope:
- EF aggregations over the ledger (non-voided rows), bucketed by the teacher's local day.
- Simple charts/tables (no drilldowns or export).

## Acceptance criteria

- [ ] A student's point history over time is viewable.
- [ ] A per-class behavior breakdown over a selectable date range is viewable.
- [ ] Aggregations exclude voided rows.
- [ ] Authorization enforced.

## Blocked by

- #6 — Points ledger

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
