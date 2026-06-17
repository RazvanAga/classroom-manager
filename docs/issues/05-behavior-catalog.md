# Slice 4 — Behavior catalog + default seeding

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Each class has a catalog of named behaviors with signed default point values; a new class is auto-seeded with a sensible default set (copied into rows), and the teacher can add / edit / remove them.

Scope:
- `Behavior` entity (ClassId, Name, DefaultPoints[signed], CreatedAt) — per-class.
- A default template held in code; on class creation, **copy template rows into the class** (not a shared FK).
- CRUD endpoints + UI.

## Acceptance criteria

- [ ] Creating a class seeds the default behavior set as editable per-class rows.
- [ ] Teacher can add, edit, and remove behaviors.
- [ ] Each behavior carries a signed default point value (positive or negative).
- [ ] Editing one class's behaviors does not affect another class.
- [ ] Authorization enforced (class members only).

## Blocked by

- #3 — Classes + Owner/membership authorization

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
