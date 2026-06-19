# Slice 20 — App shell + class selector + auth restyle

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Replace the current single-page shell with the new navigation structure and visual language. This slice establishes the skeleton every later screen plugs into.

- Adopt Tailwind CSS v4 + lucide-react, and a centralized Romanian-strings module (code stays English, UI strings live in one place).
- Design projector/touch-first: large targets, high contrast, readable from a distance, works on large screens and tablets.
- Class-scoped routing: a class is selected via the URL, with a persistent per-class top nav of five sections — Dashboard, Magazin, Grupuri, Rapoarte, Setări — and a class header showing the class name, its currency icon, and a Kiosk button.
- The root page is a class selector: the teacher's classes as cards (name, role, student count, currency icon) with create + archive. Selecting a card enters that class.
- Restyle the login and "Try the demo" pages to match.

The five section pages can be placeholders in this slice; later slices fill each one. The architecture stays client components + TanStack Query over the existing REST client (no Server Actions, no extra runtime) so the static export still works behind Nginx.

## Acceptance criteria

- [ ] The frontend uses Tailwind v4 + lucide; Romanian UI strings live in one module; identifiers/comments stay English.
- [ ] The root page lists the teacher's classes as cards (name, role, student count) and supports create + archive; selecting one routes into the class.
- [ ] Inside a class, a persistent nav exposes the five sections, and a class header shows name + currency icon + a Kiosk entry.
- [ ] Login and demo-login are restyled and functional; the demo lands in "Clasa Steluțelor".
- [ ] The production static export still builds (no Node runtime required in prod).

## Blocked by

- #19 — Per-class currency icon

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
