# Slice 14 — Student data purge (erasure)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A teacher can permanently purge a student's data on request (erasure of a minor's data), **anonymizing** rather than orphaning their ledger rows so aggregate integrity is preserved.

Scope:
- A hard-purge path distinct from (and stronger than) soft-delete.
- Anonymize the student's ledger references; remove PII, avatar, and inventory.

## Acceptance criteria

- [ ] Purging a student removes their PII and avatar/inventory.
- [ ] The student's ledger rows are anonymized, not orphaned (aggregate integrity preserved).
- [ ] Purge is distinct from and stronger than soft-delete.
- [ ] Authorization enforced (Owner/appropriate role).

## Blocked by

- #6 — Points ledger

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
