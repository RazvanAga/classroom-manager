# Slice 3 — Roster: single add + bulk-paste, soft-delete

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Within a class, a teacher adds students one-by-one or by pasting a multi-line list; gender is optional; students can be soft-deleted (preserving history), and a soft-deleted student is hidden everywhere via a global query filter.

Scope:
- `Student` entity (ClassId, DisplayName, **Gender** nullable enum `{Female, Male}`, CreatedAt, DeletedAt?).
- **Bulk-paste parser** (pure, unit-tested): lines of `Name` or `Name, F/M`; tolerant of malformed lines.
- Soft-delete + **EF global query filter** on `DeletedAt`.
- Roster UI (list, single add, paste, remove).

## Acceptance criteria

- [ ] Single add creates a student in the class.
- [ ] Pasting a list creates the expected students with parsed gender (lines without a marker get null gender).
- [ ] The parser is covered by unit tests (valid lines, optional gender token, malformed lines).
- [ ] Soft-deleted students are excluded from the roster and all queries via the global filter.
- [ ] Only class members can modify the roster (authorization enforced).

## Blocked by

- #3 — Classes + Owner/membership authorization

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
