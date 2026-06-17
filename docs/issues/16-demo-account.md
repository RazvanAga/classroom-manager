# Slice 15 — Demo account + scheduled reset + "Try demo" login

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A one-click "Try demo" login into a writable, fully-interactive demo account that a scheduled job periodically re-seeds to a rich, populated state (a full class with varied point histories and owned avatars), so reviewers always land on something impressive regardless of prior visitors.

Scope:
- A seeded demo account with rich seed content (class, students, varied histories, owned/equipped avatars).
- A scheduled (cron) **re-seed** job that resets the demo to the known rich state.
- A prominent "Try demo" login control.

## Acceptance criteria

- [ ] "Try demo" logs a reviewer into the demo account in one click.
- [ ] The demo is fully interactive (award points, shop, run tools).
- [ ] A scheduled job re-seeds the demo to a known rich state.
- [ ] After re-seed, the demo shows a populated class with histories and avatars.

## Blocked by

- #9 — Store + transactional purchase
- #11 — Random student picker
- #12 — Random group maker

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
