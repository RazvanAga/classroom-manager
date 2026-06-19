# Slice 25 — Setări redesign

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A sectioned class-settings page that gathers the per-class administration in one place:

- **Behavior catalog** — add, edit, and remove behaviors.
- **Roster** — single add, bulk paste, and soft-remove students.
- **Purge** — permanently erase a student (Owner only) behind a clear, irreversible-action confirmation.
- **Kiosk PIN** — set the per-teacher exit PIN.
- **Currency icon** — choose the class's reward icon from the fixed set.
- **Archive** — archive the class.

Reuses the existing endpoints; this slice is the unified UI plus the currency-icon picker wired to the field from #19.

## Acceptance criteria

- [ ] The behavior catalog can be created, edited, and removed.
- [ ] The roster supports single add, bulk paste, and soft-remove.
- [ ] A student can be purged (Owner only) behind a clear irreversible-action confirmation.
- [ ] The kiosk PIN can be set; the currency icon can be chosen from the fixed set; the class can be archived.
- [ ] Destructive / Owner-only actions are authorized and fail closed for non-owners.

## Blocked by

- #19 — Per-class currency icon
- #21 — App shell + class selector

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
