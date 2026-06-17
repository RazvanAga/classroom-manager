# Slice 5 — Points ledger: award/deduct (single + bulk) + wallet/lifetime + leaderboard

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Teachers award or deduct points to one student or many at once using behaviors; every change is an immutable ledger row; each student shows a spendable **wallet** and a **lifetime-earned** total; the class leaderboard ranks by lifetime earned.

Scope:
- `PointTransaction` entity (StudentId, Amount[signed], Type[`Award`|`Deduction`|`Purchase`|`Adjustment`], BehaviorId?, AwardedByTeacherId?, ItemId?, BatchId?, Reason?, CreatedAt, VoidedAt?, VoidedByTeacherId?). This slice uses `Award`/`Deduction`.
- **Bulk award**: select students (or whole class) → one row per student sharing a `BatchId`.
- **Wallet** = `SUM(amount)` over non-voided rows. **Lifetime earned** = `SUM(amount)` over non-voided rows where amount > 0 and Type = `Award`.
- Leaderboard + per-student balance UI.

## Acceptance criteria

- [ ] Awarding a behavior to a student creates a ledger row and updates wallet + lifetime.
- [ ] Bulk award to multiple/all students creates one row each sharing a batch id.
- [ ] Wallet reflects awards and deductions; lifetime reflects only positive awards.
- [ ] Leaderboard ranks by lifetime earned.
- [ ] Aggregation rules (wallet vs lifetime) covered by unit tests.
- [ ] Authorization enforced.

## Blocked by

- #4 — Roster
- #5 — Behavior catalog

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
