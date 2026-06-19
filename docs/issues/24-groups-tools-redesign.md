# Slice 23 — Grupuri (picker + groups + timer)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

The class-tools page, with three sections on one page (as in the prototype).

- **Random student picker:** calls the server-authoritative fair picker (no-repeat-until-cycle; the client carries the already-picked set), presented with a spinning-wheel reveal animation and a "recently picked" list. The fairness logic stays on the server — the animation is presentation only.
- **Group maker:** forms groups by group size, with optional gender balancing; lets the teacher award a behavior to a whole group (one bulk batch); and can save and re-open named groupings (reproduced from the saved seed).
- **Timer:** a client-only countdown with a circular SVG progress ring, presets and a custom duration, start/pause/reset, and a Web Audio chime at zero.

"Alege Lideri" is deliberately out of scope (deferred).

## Acceptance criteria

- [ ] Picking a student uses the server fair picker (no repeats until everyone is picked) with an animated reveal and a recent-picks list.
- [ ] Groups form by group size with optional gender balance; a grouping can be saved and re-opened identically.
- [ ] A behavior can be awarded to an entire group in one batch.
- [ ] The timer counts down with a circular progress ring, supports presets + custom time, pause/resume/reset, and chimes at zero.

## Blocked by

- #21 — App shell + class selector

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
