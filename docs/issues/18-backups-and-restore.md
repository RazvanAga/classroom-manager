# Slice 17 — Backups + documented restore

**Type:** HITL

## Parent

#1 — PRD: Classroom Manager

## What to build

Nightly off-box backups of Postgres and the DataProtection keys, with a committed, verified restore procedure.

Scope:
- Nightly `pg_dump` (compressed, rotated, e.g. 7 daily + 4 weekly) **plus** a copy of the DataProtection key directory.
- Push both **off-box** to S3-compatible storage (e.g. Hetzner Storage Box / Object Storage).
- A restore runbook committed to the repo.

HITL: requires off-box storage credentials and decisions.

## Acceptance criteria

- [ ] A nightly job dumps Postgres (compressed, rotated) and copies the key directory.
- [ ] Backups are pushed off-box to S3-compatible storage.
- [ ] A documented restore procedure is committed and verified at least once.

## Blocked by

- #17 — Production deployment to Hetzner

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
