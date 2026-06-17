# Slice 2 — Classes + Owner/membership authorization

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

A logged-in teacher creates classes and sees their class list; classes can be archived; co-teachers can be added with an Owner/Collaborator role; a teacher cannot access classes they don't belong to, and only Owners can perform destructive operations.

Scope:
- `Class` entity (Name, CreatedAt, IsArchived, DeletedAt?), GUIDv7 primary keys.
- `ClassTeacher` join (ClassId, TeacherId, Role[Owner|Collaborator]); the creator becomes Owner.
- **Resource-based authorization**: a membership handler (current teacher must be in `ClassTeacher` for the target class) and an Owner handler (gates delete class, add/remove teacher).
- Endpoints: create / list / archive class, add / remove co-teacher.
- UI: class list + create.

## Acceptance criteria

- [ ] Creating a class makes the creator the Owner.
- [ ] A teacher sees only classes they are a member of.
- [ ] A non-member is denied (cross-tenant) on both read and write — covered by an integration test.
- [ ] A Collaborator cannot delete the class or add/remove teachers; an Owner can.
- [ ] A class can be archived and is excluded from the active list.

## Blocked by

- #2 — Local walking skeleton

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
