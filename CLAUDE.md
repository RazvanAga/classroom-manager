# CLAUDE.md

Guidance for AI agents working in this repo. Keep this file lean — it loads every session.

## What this is

Classroom Manager — a single-teacher classroom app (behavior points, customizable avatars + store, random picker/groups, timer). It is a **portfolio project targeting .NET roles**: built to single-device scale (≈1 concurrent user on a teacher's laptop) but engineered to *demonstrate* craftsmanship. Don't "simplify away" the architecture/authz/testing rigor for scale reasons — that rigor is the point. Equally, don't add scale machinery (caching layers, sharding, SignalR) it doesn't need.

## Read first (canonical sources — don't duplicate them, update them)

- **docs/PRD.md** — what we're building (problem, user stories, decisions). Mirrored to GitHub issue #1.
- **docs/design.md** — *why*, with rejected alternatives. Read before changing any decision.
- **docs/issues/** — work sliced into vertical "tracer-bullet" tickets, mirrored to GitHub issues #2–#18. **docs/issues/README.md** has the dependency graph.

If a decision in code conflicts with these docs, stop and reconcile rather than guessing.

## Status

Issues **#2–#15 complete** (walking skeleton, classes + authz, roster, behavior catalog, points ledger + wallet/lifetime + leaderboard, undo via soft-void, avatar foundation: global DiceBear `adventurer` catalog + free defaults granted on student creation + free equip, store + transactional purchase: rarity-priced catalog, serializable purchase txn with retry + in-txn re-sum, double-buy blocked by the `(StudentId, ItemId)` unique PK, insufficient funds → 402, kiosk mode: distinct `classroom.kiosk` cookie scheme behind a policy/forward default scheme, `{mode, classId, teacherId}` claims, enter signs the teacher out + mints the reduced-scope session, hashed per-teacher exit PIN restores the teacher, `KioskOrMemberRequirement` widens only roster-read/shop/equip while every teacher/admin endpoint fails closed → 403, random student picker: pure seedable `FairPicker.Pick(eligible, alreadyPicked, seed)` domain function — no-repeat-until-everyone cycle with on-demand reset, stateless/no DB table (client carries the per-cycle set, server-authoritative roster + fresh seed per request, member-only/kiosk-rejected), random group maker: pure seedable `GroupFormer.Form(students, groupSize, balanceByGender, seed)` — `ceil(N/size)` even groups (differ by ≤1) via round-robin deal, optional gender balancing by stratified `{Female, Male, null}` buckets, `Grouping`+`GroupMember` persistence, preview returns a seed that `save` re-derives deterministically so the saved arrangement matches the preview, member-only/kiosk-rejected, activity timer: client-only countdown (no backend, design.md §6.1) — presets + custom duration, deadline-based ticking, start/pause/reset, visual flash + optional Web Audio chime at zero, reporting view: read-only teacher analytics over the non-voided ledger (design.md §9.3) — pure seedable `PointTimeline` domain fns (`ToUtcWindow`/`LocalDay`/`Build`) bucket a student's points by the teacher's **local day** via a client-supplied `tzOffsetMinutes`, filling empty days and carrying a running wallet from an opening balance; per-class behavior breakdown via an EF `GroupBy` (counts + signed point totals, most-common-first, behavior-only — adjustments/purchases excluded), range defaults to last 30 days / capped at 366, member-only/kiosk-rejected, student data purge (erasure, design.md §7.1): Owner-gated `POST /students/{id}/purge` that **anonymizes rather than orphans** — the `Student` row is kept but overwritten (name → `Student.PurgedDisplayName`, gender → null, `PurgedAt` set) instead of hard-deleted (a hard delete would cascade away the ledger), so `PointTransaction` rows stay parented and class aggregates intact; avatar (`StudentEquipped`) + inventory (`StudentOwnedItem`) + `GroupMember` placements are `ExecuteDelete`d and ledger `Reason` free-text scrubbed, all inside one transaction; `DeletedAt` set too so the existing soft-delete filter hides the tombstone — distinct from + stronger than the undoable member-level soft-delete; lookup uses `IgnoreQueryFilters` so an already-removed student can still be purged). Next in dependency order: **#16 — Demo account + scheduled reset + Try-demo login** (see docs/issues/README.md). Remaining AFK slice #16, then HITL **#17 (deploy)** / **#18 (backups)**. Build in dependency order; respect each ticket's `Blocked by`.

## Workflow conventions

- **One vertical slice at a time.** Each issue cuts end-to-end (schema → API → UI → tests) and must be demoable/verifiable on its own.
- **Commit on completion of each issue** with a descriptive message (the issue title is acceptable). **Work directly on `main` — do not create feature branches.** Commit straight to `main`.
- **Issues #2–#16 are AFK** (agent-implementable); **#17 (deploy) and #18 (backups) are HITL** — don't attempt the Hetzner deploy autonomously; deployment is deliberately deferred until the app is production-ready.
- Keep the three docs above in sync when scope changes.

## Stack

.NET 10 · ASP.NET Core Minimal APIs, **vertical-slice** layout (no MediatR) · EF Core + PostgreSQL · ASP.NET Identity (cookie auth, same-domain) · FluentValidation · ProblemDetails (RFC 9457) · Scalar docs · Next.js (App Router + TS, static export) + TanStack Query · xUnit. Hand-map DTOs (no AutoMapper).

## Invariants — do not violate without updating docs/design.md

These are the load-bearing, easy-to-get-wrong decisions:

- **Points are an append-only ledger.** Balance = sum of non-voided rows. Two derived views: **wallet** = `SUM(amount)` (non-voided); **lifetime earned** (leaderboard) = `SUM(amount)` where `amount > 0 AND Type = Award` (non-voided).
- **Undo = soft-void**, never delete and never a compensating entry. Set `VoidedAt`/`VoidedByTeacherId`; void a bulk award by its `BatchId`. *Every* aggregation must exclude voided rows.
- **Purchases run in a serializable/repeatable-read transaction** (re-sum → check `balance >= cost` → insert → commit, retry on serialization failure). `UNIQUE(StudentId, ItemId)` is the DB backstop against double-buy. Wallet may go negative via deductions but **never via a purchase**.
- **Authorization is resource-based handlers** (class membership + Owner for destructive ops). A teacher must never access a class they're not in — cover cross-tenant denial with integration tests.
- **Kiosk mode = a separate reduced-scope principal** (`{mode:kiosk, classId}`), not hidden UI. The server must reject teacher/admin endpoints for the kiosk principal. PIN to exit.
- **GUID v7 primary keys** (`Guid.CreateVersion7()`).
- **Soft-delete via an EF global query filter** on `DeletedAt` — this is the *only* place a global query filter is used (authz is handlers, not filters). `IsArchived` is a separate lifecycle flag handled as an ordinary query predicate (archived rows stay queryable so history is retained), **not** part of the global filter.
- **Avatars: one locked DiceBear style.** A store item = one option value for a slot; the frontend composes/renders. No cross-style mixing; no render metadata stored.
- **Enums stored as strings** (EF conversion). Timestamps UTC; reports bucket by the teacher's local day.
- **DataProtection keys persist to a mounted volume** (deploy slice) so cookies/antiforgery survive redeploys.

## Testing

Two seams. Assert external behavior, not internals.
1. **Integration** — drive real endpoints via `WebApplicationFactory` against real Postgres (Testcontainers), running real migrations. Primary seam for feature behavior, authz, EF mapping. The harness from #2 is the reference pattern later slices copy.
2. **Unit** — pure domain functions, no DB: group formation (size/even/gender-balance), fair-pick cycle, ledger aggregation, purchase affordability, roster paste parser.

**Local dev gotchas:**
- There is **no `.sln`** — run tests per-project: `dotnet test backend/tests/Classroom.UnitTests/Classroom.UnitTests.csproj` (and the IntegrationTests csproj).
- **Docker Desktop must be running** before integration tests — Testcontainers needs it, or every integration test fails with a misleading Docker-connection error.
- **Migrations:** the API doesn't reference `EFCore.Design`, so use Infrastructure as its own startup project: `dotnet ef migrations add <Name> --project backend/src/Classroom.Infrastructure --startup-project backend/src/Classroom.Infrastructure` (a `DesignTimeDbContextFactory` supplies the connection string).
- **New FluentValidation validators are auto-discovered** via the assembly scan in Program.cs — no manual registration.

## Notes

- No public registration / no SMTP in v1 — accounts are **seeded**; a one-click demo login exists, and a scheduled job re-seeds the writable demo to a rich state.
- `Gender` is a nullable binary enum `{Female, Male}` (owner decision; used for optional gender-balanced grouping).
- Frontend is served as static export by Nginx in prod — **no Node runtime in production**.
