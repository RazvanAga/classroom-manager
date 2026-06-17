# PRD: Classroom Manager

> Canonical copy. Also published as [GitHub issue #1](https://github.com/RazvanAga/classroom-manager/issues/1) (label `prd`).

## Problem Statement

A teacher running a classroom needs a single tool, on their own laptop, to manage their classes and keep students engaged. Today this means juggling separate things: a paper/whiteboard behavior-points tally, a sticker or reward chart, a hat-pulling name draw for "who answers next", ad-hoc group splitting, and a phone timer for activities. Nothing is connected, points history is lost, rewards aren't motivating, and forming fair groups (by size, and balanced by gender) is done by hand.

The teacher wants one app that: tracks multiple classes and their students, awards/deducts behavior points with a full history, turns those points into a fun reward loop (customizable avatars bought from a store), and bundles the everyday classroom utilities (random student picker, random group maker, activity timer) in one place.

## Solution

A classroom manager web app, used on the teacher's laptop, where a logged-in teacher manages their classes and students and runs the classroom day:

- **Points** are awarded or deducted against a per-class catalog of named behaviors (e.g. "Helped a peer" +1, "Off task" −1), to one student or many at once. Every change is an immutable ledger entry, so history, undo, and reporting come for free. Each student has a **wallet** (spendable balance) and a **lifetime-earned** total (the leaderboard score, never reduced by spending).
- **Avatars**: each student has a customizable avatar built from layered options of a single avatar style (DiceBear). Students spend wallet points in a **store** to buy avatar options and equip them.
- **Kiosk mode**: the teacher hands the laptop (or projects it) to a student, who taps their own name and shops/equips within their own inventory — without logging in and without access to teacher functions.
- **Classroom tools**: a random student picker (fair — no repeats until everyone's been picked), a random group maker (pick group size, even distribution, optional gender balancing), and an activity timer.
- **Reporting**: a lightweight analytics view — leaderboard, per-student point timeline, per-class behavior breakdown.

The app is also a portfolio piece targeting .NET roles, so the backend is built to demonstrate craftsmanship (clean vertical-slice architecture, real authorization, transactional integrity, a real test pyramid, CI/CD, and production ops) even though it serves a single concurrent user.

## User Stories

1. As a teacher, I want to log in to the app, so that only I can access my classes and students.
2. As a teacher, I want to create a class with a name, so that I can organize students into the groups I teach.
3. As a teacher, I want to be added as the Owner of a class I create, so that I have full control over it.
4. As a teacher, I want to add other teachers to a class as co-teachers, so that we can share management of the same class.
5. As a class Owner, I want only Owners to be able to delete a class or add/remove teachers, so that collaborators can't perform destructive actions.
6. As a teacher, I want to belong to multiple classes and have a class belong to multiple teachers, so that the tool reflects real co-teaching arrangements.
7. As a teacher, I want to be prevented from seeing or modifying any class I'm not a member of, so that other teachers' data stays private.
8. As a teacher, I want to add a single student to a class via a small form, so that I can quickly add a late arrival.
9. As a teacher, I want to paste a multi-line list of students (one per line, optionally "Name, F/M"), so that I can populate a whole class at once.
10. As a teacher, I want to optionally record a student's gender (Female/Male, or leave it unset), so that I can later form gender-balanced groups.
11. As a teacher, I want each new class to start with a sensible default set of behaviors, so that I can award points immediately without setup.
12. As a teacher, I want to add, edit, and remove behaviors in a class's catalog, so that the reasons match how I run my room.
13. As a teacher, I want each behavior to have a default point value (positive or negative), so that awarding is one tap.
14. As a teacher, I want to award a behavior to a single student, so that I can recognize individual actions.
15. As a teacher, I want to select several students (or the whole class) and apply a behavior at once, so that I can reward group behavior quickly.
16. As a teacher, I want every award/deduction recorded with who, what, when, and why, so that I have a full history.
17. As a teacher, I want to undo a mistaken award (including an entire bulk award), so that fat-finger mistakes don't corrupt a student's balance or the leaderboard.
18. As a teacher, I want a student's spendable wallet to reflect awards, deductions, and purchases, so that the store enforces what they can afford.
19. As a teacher, I want the leaderboard to rank by lifetime points earned (unaffected by spending), so that shopping never penalizes a student's standing.
20. As a teacher, I want to view a class leaderboard, so that I can celebrate top contributors.
21. As a teacher, I want to view a single student's point history over time, so that I can discuss their progress.
22. As a teacher, I want to see a per-class breakdown of most common positive and negative behaviors over a date range, so that I understand class dynamics.
23. As a student (via the teacher's device), I want a customizable avatar, so that I have a personal identity in the class.
24. As a student, I want to start with a free default avatar, so that I'm never faceless.
25. As a student, I want to browse a store of avatar options, so that I can choose what to buy.
26. As a student, I want to spend my wallet points to buy an avatar option, so that I'm rewarded for earning points.
27. As a student, I want to be prevented from buying something I can't afford, so that my balance can't go negative from shopping.
28. As a student, I want to not be able to buy the same item twice, so that I don't waste points.
29. As a student, I want to equip any option I own for free, and switch freely, so that I can restyle my avatar anytime.
30. As a teacher, I want to launch a kiosk mode and hand the laptop to a student, so that they can shop and equip themselves.
31. As a student in kiosk mode, I want to tap my own name from the roster without logging in, so that I can access my avatar and inventory.
32. As a teacher, I want kiosk mode to block access to teacher functions (awarding, deleting, class management), so that a student holding my laptop can't change protected data.
33. As a teacher, I want to exit kiosk mode with a short PIN, so that I can reclaim full control quickly.
34. As a teacher, I want a random student picker, so that I can call on students fairly.
35. As a teacher, I want the picker to avoid repeating a student until everyone has been picked once, then reset, so that selection feels fair.
36. As a teacher, I want to reset the picker cycle on demand, so that I can start fresh.
37. As a teacher, I want to make random groups by choosing how many students per group, so that I can split the class for an activity.
38. As a teacher, I want group sizes distributed evenly (no lone straggler group), so that groups are balanced in size.
39. As a teacher, I want to optionally balance groups by gender, so that boys and girls are spread evenly across groups.
40. As a teacher, I want students with no recorded gender still distributed evenly when balancing, so that grouping always works.
41. As a teacher, I want to optionally save a formed grouping, so that I can reuse it during the activity.
42. As a teacher, I want an activity timer with presets and start/pause/reset, so that I can time group work.
43. As a teacher, I want a clear visual (and optional sound) alert when the timer ends, so that the class knows time is up.
44. As a teacher, I want to remove a student from a class, so that I can handle a student who leaves.
45. As a teacher, I want a removed student's history preserved (soft delete) and undoable, so that an accidental removal isn't catastrophic.
46. As a teacher, I want to archive a class at year-end, so that old classes don't clutter the active list but history is kept.
47. As a teacher, I want a way to permanently purge a student's data on request, so that I can honor erasure of a minor's data.
48. As a reviewer/recruiter, I want to try the app via a one-click demo login, so that I can explore it without creating an account.
49. As a reviewer, I want the demo to be fully interactive (award points, buy avatars, run tools), so that I experience the real product.
50. As a reviewer, I want the demo reset periodically to a rich, populated state, so that I always see something impressive regardless of prior visitors.
51. As the developer, I want consistent machine-readable error responses (ProblemDetails), so that the API looks professional and the frontend can handle errors uniformly.
52. As the developer, I want auth sessions to survive deploys, so that nobody is logged out every time I ship.
53. As the developer, I want nightly off-box backups with a documented restore, so that I can recover real classroom and demo data.

## Implementation Decisions

**Overall shape**
- Single-device app (teacher's laptop, ~one concurrent user). Clean architecture is a deliberate portfolio signal, not a scale requirement — to be stated in the README.
- Backend: C# / **.NET 10 (LTS)**, ASP.NET Core, **vertical-slice architecture** (organize by feature; endpoints call handlers directly; **no MediatR**). A Domain layer holds entities and the pure algorithms; EF Core + PostgreSQL live in Infrastructure.
- API style: **Minimal APIs** grouped per slice via `MapGroup`. Validation via **FluentValidation**. **Hand-mapped DTOs** (no AutoMapper). **ProblemDetails (RFC 9457)** for all error responses including validation. **Scalar** over the OpenAPI document. Structured JSON logging via the built-in `ILogger` to stdout.
- Frontend: **Next.js (App Router + TypeScript)** used as a client-side SPA shell; **TanStack Query** is the primary data layer (cache invalidation in place of realtime; optimistic updates for points/store). Built with **static export** and served by **Nginx** — no Node runtime in production. **Polished playful-but-clean** visual direction with a small custom design system (design tokens + a few components), not stock component-library styling.
- Avatars rendered client-side via **DiceBear**, **locked to a single style**; avatar "slots" are that style's option groups; a store item = one option value within a group. No render/z-order metadata stored.
- No realtime/SignalR. No polling needed (single device); the SPA invalidates/refetches after its own mutations.

**Authentication & authorization**
- **ASP.NET Core Identity**, teachers-only accounts. **No public registration in v1** — accounts are seeded (via seed/migration or a small CLI); the registration UI / invite-token signup is a stretch goal. **No SMTP** in v1 (no email confirmation/reset).
- **Cookie auth**, same-domain: API and static frontend served under one domain behind Nginx; HttpOnly + SameSite cookies; **antiforgery** on mutations.
- **DataProtection keys persisted to a mounted Docker volume** with a fixed application name, so cookies/antiforgery survive container redeploys.
- Authorization: **resource-based authorization handlers** — a membership handler checks the current teacher is in `ClassTeacher` for the target class; an Owner handler gates destructive ops (delete class, add/remove teacher).
- **Kiosk mode** mints a **separate, short-lived, reduced-scope session/principal** carrying claims like `{ mode: kiosk, classId }`. Authorization policies allow this principal **only** roster-read + shop/equip for that class; all teacher/admin policies reject it server-side. Exiting requires a short per-teacher numeric **PIN**.

**Domain: points (append-only ledger)**
- Points are an **append-only transaction ledger**. Each row: student, signed amount, type (`Award` | `Deduction` | `Purchase` | `Adjustment`), optional behavior, optional awarding teacher, optional item (for purchases), optional batch id (for bulk awards), optional reason, timestamp, and **soft-void fields** (`VoidedAt`, `VoidedByTeacherId`).
- **Wallet (spendable)** = `SUM(amount)` over non-voided rows. **Lifetime earned (leaderboard)** = `SUM(amount)` over non-voided rows where amount > 0 and type = `Award`. Both derive from one ledger.
- **Undo** = soft-void: set void fields on the row(s); all aggregations exclude voided rows. A bulk award is undone by voiding all rows sharing its batch id. (No compensating entries, no hard delete.)
- **Negative wallet** is allowed via deductions (it's a behavior score); the **store purchase** is the only operation blocked below zero.
- **Bulk award**: selecting multiple students (or whole class) writes one ledger row per student, tagged with a shared batch id.

**Domain: store & avatar**
- **Global avatar catalog**, fixed seeded prices, seeded by enumerating the chosen DiceBear style's options. Item carries: slot/option-group, option value, cost, optional rarity (price-tier label), `IsDefault`, display name.
- New students automatically **own the free default items**.
- **Purchase** is a **serializable (or repeatable-read) transaction**: re-sum the student's balance, verify `balance >= cost`, insert the `Purchase` ledger row, insert the ownership row, commit; **retry on serialization failure**. A **`UNIQUE(StudentId, ItemId)`** constraint is the DB-level backstop against double-buy. (The "balance never negative" invariant is cross-row and rests on the transaction, not a CHECK.)
- **Equipping** is free and just sets the equipped option per slot for the student.

**Domain: classroom tools**
- **Group formation** and **fair pick** are **pure, seedable functions** in the Domain layer (deterministic given a seed).
- Group formation input: **group size**; members distributed so groups differ by at most one. **Optional gender balancing**: stratify students into `{Female, Male, null}` buckets and round-robin/snake-deal each bucket across groups so each (including unrecorded) spreads evenly. Output optionally persisted as a grouping.
- **Fair pick**: `(eligibleStudents, alreadyPickedIds, seed) → nextStudent`; "fair" = no repeat until everyone is picked once, then the cycle resets. The already-picked set is held per session (no DB table); reset clears it.
- **Timer**: client-side only (countdown, presets, start/pause/reset, visual + optional sound alert). No backend.
- **Roster bulk-paste parser**: parses lines of `Name` or `Name, F/M` into student records; a pure, unit-testable function.

**Schema (entities; GUIDv7 primary keys via `Guid.CreateVersion7`)**
- `ApplicationUser` (Identity) = teacher.
- `Class` (Name, CreatedAt, IsArchived, DeletedAt?).
- `ClassTeacher` (ClassId, TeacherId, Role[Owner|Collaborator]) — composite PK.
- `Student` (ClassId, DisplayName, **Gender** nullable enum `{Female, Male}`, CreatedAt, DeletedAt?).
- `Behavior` (ClassId, Name, DefaultPoints[signed], CreatedAt) — per-class; seeded from a code template on class creation (template copied into rows, not a shared FK).
- `PointTransaction` (StudentId, Amount[signed], Type, BehaviorId?, AwardedByTeacherId?, ItemId?, BatchId?, Reason?, CreatedAt, VoidedAt?, VoidedByTeacherId?).
- `AvatarItem` (Slot, OptionValue, Cost, Rarity?, IsDefault, DisplayName) — single style.
- `StudentOwnedItem` (StudentId, ItemId) — `UNIQUE(StudentId, ItemId)`.
- `StudentEquipped` (StudentId, Slot, ItemId) — PK(StudentId, Slot).
- `Grouping` (ClassId, CreatedByTeacherId, GroupSize, CreatedAt) and `GroupMember` (GroupingId, GroupNumber, StudentId) — optional persistence.
- **Enums stored as strings** in Postgres (EF conversion). **Soft delete via EF global query filter** on `DeletedAt`/`IsArchived` (the one place a query filter is used — distinct from the authz handlers). Timestamps stored UTC; reports bucket by the teacher's local day.

**Delivery & ops**
- Containers: multi-stage Dockerfile for the API (SDK build → aspnet runtime, non-root user). Production Compose stack = **Nginx + API + Postgres** (frontend is static assets served by Nginx).
- **CI/CD (GitHub Actions)**: build, run unit + integration tests (Testcontainers works on hosted runners), build images, push to **GHCR**, then SSH to the Hetzner VPS for `docker compose pull && up`.
- **EF migrations** applied via a **migration bundle run as an explicit deploy step** (not auto-on-startup).
- **Secrets**: GitHub Actions secrets → a root-only `.env` on the VPS referenced by Compose; never baked into images.
- **Backups**: nightly `pg_dump` (compressed, rotated, e.g. 7 daily + 4 weekly) **plus** a copy of the DataProtection key directory, pushed **off-box** to S3-compatible storage; a documented restore procedure committed to the repo.
- **Demo**: a writable seeded demo account; a scheduled job (cron) **re-seeds it to a rich populated state** (full class, varied point histories, owned avatars). One-click "Try demo" login.
- Login brute-force handled by **Identity's built-in lockout**. **MIT LICENSE**. README notes the deliberate-over-engineering-as-portfolio-signal framing.

## Testing Decisions

Good tests assert **external behavior**, not implementation details — the same test should survive a refactor of the internals. Two seams (both new; placed at the highest practical points):

**1. HTTP/API seam — integration tests.** Drive real endpoints over HTTP via `WebApplicationFactory` against a **real PostgreSQL** started with **Testcontainers**, running real EF migrations. This is the primary seam for feature behavior, asserting on HTTP status + response bodies + resulting DB state:
- Auth: protected endpoints reject unauthenticated requests; seeded login works.
- **Authorization**: a teacher cannot read or mutate a class they don't belong to (cross-tenant denial); a Collaborator cannot perform Owner-only destructive ops.
- **Kiosk scope**: a kiosk principal can read roster + shop/equip but is rejected (403) on award/deduct/delete/class-management endpoints.
- **Points**: award/deduct updates wallet and lifetime correctly; bulk award writes one row per student with a shared batch id; void (single and by-batch) removes rows from both aggregations.
- **Purchase integrity**: a purchase the student can't afford is rejected; buying the same item twice is rejected (unique constraint); concurrent purchase attempts don't drive the balance negative.
- **Roster**: bulk paste creates the expected students (with parsed gender); single add works.
- **Grouping endpoint**: returns evenly-sized groups for a given size; gender-balanced option spreads buckets evenly; persisted grouping is retrievable.
- **Lifecycle**: soft-deleted students/archived classes are excluded by the global query filter; purge removes/anonymizes as specified.

**2. Pure-domain-function seam — unit tests (no DB).** Call the pure algorithms directly with constructed inputs and a fixed seed:
- Group formation: exact division, remainder spread (groups differ by ≤1), fewer students than group size, gender-balanced distribution across `{Female, Male, null}`.
- Fair pick: no repeat within a cycle, correct reset behavior, determinism under a fixed seed.
- Ledger aggregation rules: wallet vs lifetime computation, void exclusion.
- Purchase affordability rule.
- Roster paste parser: valid lines, optional gender token, malformed lines.

Prior art: none yet (greenfield). The integration-test harness (`WebApplicationFactory` + Testcontainers Postgres fixture) becomes the reference pattern that later feature slices copy.

## Out of Scope

- Student-owned logins / per-student authentication (kiosk mode covers student interaction).
- SignalR / realtime / multi-device sync (single-device app).
- Public self-service registration, email confirmation, and password reset (SMTP) — accounts are seeded in v1.
- Per-class store curation/repricing; teacher-defined catalogs (global fixed catalog only).
- Badges / levels / achievements / streaks (designated first stretch goal).
- Multiple DiceBear styles / a "character base" purchase (single style only).
- CSV roster upload (bulk paste only in v1).
- Rich analytics dashboards with drilldowns/exports (lightweight reporting only).
- Health-check endpoint, `/v1` URL versioning, request correlation IDs, OpenTelemetry (noted stretch/ops polish).
- Hetzner whole-VM snapshots as the primary backup (explicit scripted `pg_dump` + off-box is the chosen approach).

## Further Notes

- First milestone is a **tracer bullet**: log in as a seeded teacher → create a class → add students → render the roster, shipped through the **full CI/CD pipeline** to Hetzner (with Testcontainers integration tests and the migration-bundle deploy step) before any feature depth. Once the rails exist, every later feature (points ledger → leaderboard → store/avatar/kiosk → tools) is a low-risk additive slice.
- Suggested build order after the tracer bullet: behaviors + points ledger → leaderboard/wallet → reporting → store + avatar + kiosk → classroom tools (picker, grouping, timer) → demo seeding/reset.
- Every major decision has a defensible rationale and a considered rejected alternative, which is itself intended as a portfolio artifact and interview aid.
