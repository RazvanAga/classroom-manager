# Slice 21 — Dashboard (grid + leaderboard + award + undo)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

The class dashboard — the primary screen a teacher uses all day.

- A grid of student cards: avatar, first name, current spendable wallet shown with the class currency icon, and rank.
- An all-time leaderboard ranked by lifetime earned (no period tabs).
- Awarding via behavior chips: clicking a student opens a quick-action modal of the class's behaviors as chips (green positive / red negative) with an optional note, applied to that student.
- A multi-select mode applies one behavior to several students as a single, undoable batch.
- A "Toată clasa" shortcut applies one behavior to the whole class.
- A "Recent activity" panel lists recent transactions with undo — soft-void a single entry or a whole batch.

Awarding goes through the existing catalog-behavior award path so it counts toward lifetime/leaderboard; manual ad-hoc adjustments are out of scope for the main flow.

## Acceptance criteria

- [ ] The student grid shows each student's avatar, spendable wallet (with currency icon), and rank.
- [ ] The leaderboard ranks by lifetime earned, all-time.
- [ ] Awarding a behavior to one student, to a multi-selected subset (one batch), and to the whole class all work.
- [ ] The recent-activity panel lists transactions and can undo a single entry and a whole batch (soft-void).
- [ ] Voided entries are excluded from grid and leaderboard totals.

## Blocked by

- #21 — App shell + class selector

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
