# Implementation Issues

Vertical-slice tracer-bullet breakdown of [the PRD](../PRD.md) ([issue #1](https://github.com/RazvanAga/classroom-manager/issues/1)). Each slice cuts end-to-end and is independently demoable. Local files mirror the GitHub issues; **GitHub is the source of truth for status.**

Build order respects the `Blocked by` chains and is grouped into phases. Note that GitHub issue numbers are sequential and **do not** match build order: the Phase 2 frontend-redesign slices (#19–#27) come *before* the Phase 3 deployment gate (#17–#18), which is deliberately last. Every issue requires a git commit on completion.

## Phase 1 — Core application (#2–#16) ✅

| # | Slice | Type | Blocked by |
|---|-------|------|------------|
| [#2](https://github.com/RazvanAga/classroom-manager/issues/2) | Local walking skeleton (seeded login) | AFK | — |
| [#3](https://github.com/RazvanAga/classroom-manager/issues/3) | Classes + Owner/membership authorization | AFK | #2 |
| [#4](https://github.com/RazvanAga/classroom-manager/issues/4) | Roster (single add + bulk-paste, soft-delete) | AFK | #3 |
| [#5](https://github.com/RazvanAga/classroom-manager/issues/5) | Behavior catalog + default seeding | AFK | #3 |
| [#6](https://github.com/RazvanAga/classroom-manager/issues/6) | Points ledger + wallet/lifetime + leaderboard | AFK | #4, #5 |
| [#7](https://github.com/RazvanAga/classroom-manager/issues/7) | Undo via soft-void | AFK | #6 |
| [#8](https://github.com/RazvanAga/classroom-manager/issues/8) | Avatar foundation (DiceBear + defaults + equip) | AFK | #4 |
| [#9](https://github.com/RazvanAga/classroom-manager/issues/9) | Store + transactional purchase | AFK | #6, #8 |
| [#10](https://github.com/RazvanAga/classroom-manager/issues/10) | Kiosk mode | AFK | #9 |
| [#11](https://github.com/RazvanAga/classroom-manager/issues/11) | Random student picker (fair) | AFK | #4 |
| [#12](https://github.com/RazvanAga/classroom-manager/issues/12) | Random group maker (size, even, gender-balanced) | AFK | #4 |
| [#13](https://github.com/RazvanAga/classroom-manager/issues/13) | Activity timer | AFK | #2 |
| [#14](https://github.com/RazvanAga/classroom-manager/issues/14) | Reporting view | AFK | #6 |
| [#15](https://github.com/RazvanAga/classroom-manager/issues/15) | Student data purge (erasure) | AFK | #6 |
| [#16](https://github.com/RazvanAga/classroom-manager/issues/16) | Demo account + scheduled reset + Try-demo login | AFK | #9, #11, #12 |

## Phase 2 — Frontend redesign to prototype parity (#19–#27)

Bring the frontend up to the polish/UX of the original Vercel prototype: a class-scoped, multi-page Romanian UI (code stays English) on Tailwind v4, projector/touch-first. The backend is largely reused; only two small additions (per-class currency icon, per-student history) and a Romanian seed are net-new.

| # | Slice | Type | Blocked by |
|---|-------|------|------------|
| [#19](https://github.com/RazvanAga/classroom-manager/issues/19) | Per-class currency icon | AFK | — |
| [#20](https://github.com/RazvanAga/classroom-manager/issues/20) | Romanian seed + "Clasa Steluțelor" showcase | AFK | #19 |
| [#21](https://github.com/RazvanAga/classroom-manager/issues/21) | App shell + class selector + auth restyle | AFK | #19 |
| [#22](https://github.com/RazvanAga/classroom-manager/issues/22) | Dashboard (grid + leaderboard + award + undo) | AFK | #21 |
| [#23](https://github.com/RazvanAga/classroom-manager/issues/23) | Magazin redesign | AFK | #21 |
| [#24](https://github.com/RazvanAga/classroom-manager/issues/24) | Grupuri (picker + groups + timer) | AFK | #21 |
| [#25](https://github.com/RazvanAga/classroom-manager/issues/25) | Rapoarte + student profile | AFK | #21 |
| [#26](https://github.com/RazvanAga/classroom-manager/issues/26) | Setări redesign | AFK | #19, #21 |
| [#27](https://github.com/RazvanAga/classroom-manager/issues/27) | Kiosk redesign | AFK | #22, #23 |

## Phase 3 — Production (HITL, last) (#17–#18)

| # | Slice | Type | Blocked by |
|---|-------|------|------------|
| [#17](https://github.com/RazvanAga/classroom-manager/issues/17) | Production deployment to Hetzner (CD) | HITL | #2–#16, #19–#27 (production-ready) |
| [#18](https://github.com/RazvanAga/classroom-manager/issues/18) | Backups + documented restore | HITL | #17 |
