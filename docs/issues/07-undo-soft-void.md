# Slice 6 — Undo via soft-void (single + by-batch)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A teacher can undo a mistaken award or deduction — including an entire bulk award — without corrupting balances or the leaderboard. Undo is a **soft-void** (no deletion, no compensating entry); voided rows are retained for audit but excluded from every aggregation.

Scope:
- Wire the `VoidedAt` / `VoidedByTeacherId` behavior on `PointTransaction`.
- Void a single transaction; void a whole bulk award by `BatchId`.
- All wallet/lifetime/report aggregations exclude voided rows.

## Acceptance criteria

- [ ] Voiding a transaction removes it from wallet and lifetime.
- [ ] Voiding a bulk award by batch id voids all of its rows.
- [ ] Voided rows are retained (audit trail) but excluded everywhere.
- [ ] Void exclusion covered by tests.
- [ ] Authorization enforced.

## Blocked by

- #6 — Points ledger

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
