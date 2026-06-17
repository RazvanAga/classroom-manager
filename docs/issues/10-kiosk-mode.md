# Slice 9 — Kiosk mode

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A teacher launches kiosk mode and hands over the laptop; a student taps their own name (no login) and shops/equips within their own inventory; teacher/admin actions are rejected for the kiosk principal; the teacher exits with a PIN.

Scope:
- Entering kiosk mints a **short-lived, reduced-scope principal/session** carrying claims like `{ mode: kiosk, classId }`; a per-teacher numeric **PIN** is required to exit.
- **Authorization**: the kiosk principal is allowed only roster-read + shop/equip for that class; all teacher/admin endpoints reject it (403).
- Kiosk UI: roster of names → selected student's shop/equip; lock/exit with PIN.

## Acceptance criteria

- [ ] Entering kiosk produces a reduced-scope session scoped to the class.
- [ ] In kiosk, a student can browse/shop/equip for the selected student only.
- [ ] Award/deduct/delete/class-management endpoints reject the kiosk principal (403) — covered by an integration test.
- [ ] Exiting kiosk requires the correct PIN and restores the full teacher session.

## Blocked by

- #9 — Store + transactional purchase

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
