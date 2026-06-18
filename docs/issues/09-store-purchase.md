# Slice 8 — Store + transactional purchase

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A student spends wallet points in the store to buy avatar options; purchases are transactionally safe (no overspend, no double-buy) and the bought option enters their inventory.

Scope:
- Store lists catalog items with costs (excluding already-owned).
- **Purchase** runs in a **serializable (or repeatable-read) transaction**: re-sum the student's balance, verify `balance >= cost`, insert a `Purchase` ledger row (negative), insert `StudentOwnedItem`, commit; **retry on serialization failure**.
- `UNIQUE(StudentId, ItemId)` is the DB-level backstop against double-buy. Wallet cannot go below zero via a purchase.

## Acceptance criteria

- [x] A purchase the student can afford succeeds: wallet decreases, item owned.
- [x] A purchase exceeding the wallet is rejected (ProblemDetails — 402 Payment Required).
- [x] Buying the same item twice is rejected (unique constraint — 409 Conflict).
- [x] Concurrent purchase attempts cannot drive the balance negative — covered by an integration test.

## Blocked by

- #6 — Points ledger
- #8 — Avatar foundation

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
