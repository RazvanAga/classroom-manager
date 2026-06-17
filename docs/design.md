# Design Decisions & Rationale — Classroom Manager

Companion to [PRD.md](./PRD.md). The PRD records **what** is being built; this document records **why**, and — just as importantly — **which alternatives were rejected and why**. It doubles as the reasoning record an implementer (human or agent) can read to understand intent, and as an interview aid.

Format: each decision states the **choice**, the **reasoning**, and the **rejected alternatives**. Decisions are grouped by area, roughly in the order they were resolved (each tends to constrain the next).

> Framing note carried throughout: this is a **single-device app** (one teacher's laptop, ~one concurrent user). The clean architecture, real authorization, transactional integrity, and test/CI/ops rigor are **deliberate portfolio signals targeting .NET roles**, not responses to scale. This is stated plainly so the work doesn't read as accidental over-engineering.

---

## 1. Domain & tenancy

### 1.1 Authentication scope: teachers only
**Choice.** Only teachers have accounts (ASP.NET Core Identity). Students are entities owned by a class, not users.

**Reasoning.** Avoids the real-world complexity of authenticating minors (no school emails, parental consent, password resets for kids) while keeping Identity meaningful. Student interaction happens through **kiosk mode** (§5.4) instead.

**Rejected.**
- *Teachers + students* — student logins (join-code/PIN) are more realistic but reintroduce minor-account complexity for little portfolio gain.
- *Teachers + students + school/org admin* — the fullest multi-tenant SaaS story, but the most auth/schema surface to build and secure for a single-user tool.

### 1.2 Teacher ↔ Class: many-to-many with an Owner role
**Choice.** A teacher can be in many classes; a class can have many teachers. A `ClassTeacher` join carries a role — `Owner` (created it; can delete it / manage teachers) vs `Collaborator`.

**Reasoning.** Reflects real co-teaching, and gives a genuine **authorization story** (role-gated destructive ops) — exactly what .NET interviewers probe.

**Rejected.**
- *Many-to-many, all equal* — simpler, but no role/permission design to demonstrate.
- *One teacher owns a class (one-to-many)* — simplest, but contradicts "at least one teacher" implying many, and shows the least relational/auth depth.

### 1.3 Student ↔ Class: one class per student
**Choice.** A student belongs to exactly one class (homeroom model).

**Reasoning.** Points, avatar, and inventory belong unambiguously to that student-in-that-class. Avoids having to decide whether points/inventory are per-class or global.

**Rejected.** *Many-to-many (student in several classes)* — realistic for secondary school, but forces the per-class-vs-global ambiguity for points and inventory, adding a join table and complexity with little payoff here.

---

## 2. Points

### 2.1 Append-only transaction ledger
**Choice.** Every award/deduction/purchase is an immutable row (`PointTransaction`). Balance is the sum.

**Reasoning.** History, audit, undo, and per-behavior reporting come for free. **One elegant consequence:** both *lifetime earned* (sum of positive awards → the leaderboard score) and *spendable wallet* (net of everything) derive from the **same ledger** — one source of truth, two views. Shopping therefore never dents a student's standing. Strong demonstration of EF aggregation and a clean domain.

**Rejected.**
- *Mutable balance column* — trivial reads, but no history/audit, hard to undo, and concurrent awards risk lost updates. Weak interview story.
- *Ledger + cached balance column* — premature read-optimization at this scale (revisited and rejected again under concurrency, §4.1 — the transaction, not a cache, is what we leaned on).

### 2.2 Per-class behavior catalog + bulk award
**Choice.** Each class has a `Behavior` catalog (named reasons with signed default values). Awards can target many students (or the whole class) at once — one ledger row per student, tagged with a shared `BatchId`.

**Reasoning.** Mirrors how teachers actually run a room (ClassDojo-like), enables per-behavior analytics, and the batch id makes bulk **undo** clean (§2.4).

**Rejected.**
- *Free-form reason only* — no catalog, no per-behavior analytics, clunkier UX.
- *Catalog, single-student only* — tedious in a real classroom.

### 2.3 Seed default behaviors on class creation
**Choice.** On class creation, **copy** a curated default behavior set (in code) into the class as normal per-class rows. Fully editable thereafter.

**Reasoning.** Class is awardable from second one (good first-run UX), and the seed is a clean little use case.

**Rejected.**
- *Empty catalog* — worse first-run; easy "can't award yet" dead end.
- *Shared global behaviors via FK* — contradicts per-class customization; edits would leak across classes.

### 2.4 Undo = soft-void (not deletion, not compensation)
**Choice.** Undo sets `VoidedAt`/`VoidedByTeacherId` on the row(s); a bulk award is undone by voiding all rows sharing its `BatchId`. **Every** aggregation excludes voided rows.

**Reasoning.** This is the subtle one. The ledger is append-only and *lifetime earned* = sum of positive awards. A **compensating −5** would offset the wallet but the original +5 would *still* count toward lifetime → an inflated leaderboard. A soft-void keeps **both** views correct and preserves a full audit trail. It's a mild bend of "append-only," but it's the same soft-delete-on-a-row pattern used for students (§7.1), so it's consistent.

**Rejected.**
- *Compensating reversal entry* — purest append-only, but the leaderboard query must special-case "positive awards minus reversals-of-awards" — more complex and easier to get subtly wrong.
- *Hard delete* — instantly correct but throws away the audit trail and contradicts the soft-delete philosophy used elsewhere.

### 2.5 Negative balances
**Choice.** The wallet *may* go negative via deductions (it's a behavior score). The **store purchase** is the only operation blocked below zero.

**Reasoning.** Matches classroom reality (a kid can be "in the red" on behavior) while keeping the economy sound.

---

## 3. Avatar & store

### 3.1 Layered slots, not whole avatars
**Choice.** An avatar is a set of equipped items, one per slot. The store sells individual options per slot.

**Reasoning.** Maximizes shop variety and yields a clean catalog/inventory/equip domain. The frontend composes layers; **no image processing in .NET**.

**Rejected.**
- *Whole pre-made avatars* — trivial to model but the "customization" feature becomes shallow (a list of skins).
- *Procedural trait/color config* — cheap on assets but leaves little to buy, undermining the store.

### 3.2 DiceBear, locked to a single style
**Choice.** Use the permissively-licensed **DiceBear** library, **one style** (e.g. `adventurer`). "Slots" are that style's option groups; a store item = one option value. DiceBear handles layering/z-order internally, so **no render metadata is stored**.

**Reasoning.** Zero art production → 100% focus on the .NET domain. Crucial constraint surfaced during design: DiceBear does **not** allow mixing parts across styles — you pick one style and customize its options. Locking to one style keeps the catalog trivial and the look coherent.

**Rejected.**
- *Multiple styles as a purchasable "character base"* — richer store, but inventory/equipped state must be scoped per style and switching bases changes the whole option universe — notably more modeling/UI.
- *Self-hosted custom layered SVGs* — full z-order control and free mixing, but reopens the art-production cost we explicitly avoided.

### 3.3 Global catalog, fixed seeded prices
**Choice.** One shop for everyone; items + prices in a seeded catalog. New students own free default items so they're never faceless. Equipping is free.

**Reasoning.** Simplest correct economy; focuses backend effort on purchase integrity. Per-class curation is obvious future work.

**Rejected.**
- *Global catalog, teacher toggles/reprices per class* — richer, but adds override tables and edge cases (a kid owns a now-disabled item).
- *Fully per-class catalogs* — maximum work, mostly duplicated items.

---

## 4. Integrity under concurrency

### 4.1 Overspend/double-buy guard: serializable transaction + unique constraint
**Choice.** Purchase runs in a **serializable (or repeatable-read) transaction**: re-sum balance → verify `balance >= cost` → insert the `Purchase` row → insert ownership → commit, **retrying on serialization failure**. A `UNIQUE(StudentId, ItemId)` constraint is the DB-level backstop against double-buy.

**Reasoning.** Correct without denormalization, and a strong talking point about isolation levels. **Precision note:** the unique constraint is the *real* DB-level guarantee (against double-buy); the "balance never negative" invariant is *cross-row* and a `CHECK` cannot express it — that invariant rests entirely on the serializable transaction + retry. Knowing where the guarantee actually lives matters.

**Rejected.**
- *Optimistic concurrency on a cached balance column* — fast reads and a clean demo, but now the cached balance and the ledger must be kept consistent on every write path.
- *Idempotency key + advisory lock* — handles double-tap elegantly, but advisory locks are easy to misuse and don't stop a deduction race unless the lock covers all balance-changing paths.

> Note: with the single-device constraint (§9), real concurrency is near-nil, so this is primarily a correctness/portfolio demonstration — but it's the right way regardless.

---

## 5. Auth, authorization & kiosk

### 5.1 Cookie auth, same-domain
**Choice.** API and (static) frontend served under one domain behind Nginx; ASP.NET Identity **cookie auth** (HttpOnly + SameSite) with **antiforgery** on mutations.

**Reasoning.** No token in JS → immune to token-theft XSS; the documented first-party-SPA pattern; simplest secure setup given we already control the reverse proxy.

**Rejected.**
- *JWT bearer* — the "classic" SPA/mobile pattern, but requires refresh-rotation and safe token storage; more moving parts for no benefit on one domain.
- *BFF (Next.js proxies and holds the cookie)* — most secure, but adds a proxy layer; unnecessary once everything is same-domain.

### 5.2 No public registration / no SMTP in v1
**Choice.** Accounts are **seeded** (you + a demo account) via seed/migration or a small CLI; a one-click demo login exists. No email confirmation or password reset → **no SMTP dependency**.

**Reasoning.** Maximizes reviewer access with zero email infra. (Chosen over the "open registration, no confirmation, + demo" option per owner preference; over "open registration with real SMTP" because deliverability/secrets/failure-modes aren't worth it for a single-user hobby app.) An invite-token signup flow is a stretch goal.

### 5.3 Authorization: resource-based handlers
**Choice.** A `ClassMembership` handler verifies the current teacher is in `ClassTeacher` for the target class; an `Owner` handler gates destructive ops. Proven by **cross-tenant denial tests**.

**Reasoning.** Explicit, centralized, textbook demonstration of ASP.NET's authorization model.

**Rejected.**
- *EF global query filter on membership* — elegant defense-in-depth, but "magic" filtering can confuse (admin/seed cases) and doesn't handle write authz or roles by itself. (A query filter *is* used, but for **soft-delete** — §7.1 — a different concern.)
- *Both (filter + handlers)* — most robust, but more to build/reason about than warranted.

### 5.4 Kiosk mode: separate reduced-scope session, PIN to exit
**Choice.** Entering kiosk mints a **distinct, short-lived, reduced-scope** principal carrying `{ mode: kiosk, classId }`. Authorization allows it **only** roster-read + shop/equip for that class; all teacher/admin endpoints reject it server-side. Exit requires a short per-teacher **PIN**.

**Reasoning.** This matters *more* under the single-device constraint: the kid is literally holding the teacher's authenticated laptop. "Hide the admin buttons" client-side would be security theater (the cookie still has full rights). A real reduced-scope principal is genuine defense-in-depth and a strong scoped-authorization talking point.

**Rejected.**
- *Client-side hide only* — trivial, but privileged endpoints stay reachable.
- *Per-student PIN per action* — reintroduces a student credential (the thing we avoided) and is clunky for young kids.

### 5.5 DataProtection key persistence
**Choice.** Persist DataProtection keys to a **mounted Docker volume** with a fixed application name (deployment slice).

**Reasoning.** Cookie auth + antiforgery are encrypted with these keys. By default they live in-container and regenerate on every redeploy → everyone logged out, antiforgery broken. A volume makes them outlive container recreation. Zero extra infra since we control the host (back the directory up — §8.4).

**Rejected.**
- *Keys in Postgres (EF key store)* — also valid and captured by DB backups; slightly more wiring; fine if we ever run >1 instance.
- *Leave default (in-container)* — breaks auth on first redeploy.

---

## 6. Classroom tools

### 6.1 Grouping & fair-pick logic in C#; timer client-side
**Choice.** Group formation and fair pick are **pure, seedable** functions in the Domain layer (ideal xUnit targets); the timer is **client-side only**.

**Reasoning.** Best balance of "testable domain" and "don't over-engineer." No value in server round-trips for a countdown.

**Rejected.**
- *All three client-side* — loses three easy unit-test targets and grouping persistence.
- *Everything server-driven incl. realtime timer (SignalR)* — cool but scope-heavy and pointless on one screen.

### 6.2 Fair pick: no-repeat cycle, stateless pure function
**Choice.** `(eligibleStudents, alreadyPickedIds, seed) → next`; "fair" = no repeat until everyone is picked once, then reset. The already-picked set is held **per session** (no DB table); reset clears it.

**Reasoning.** Keeps the function pure and trivially testable; avoids a table.

**Rejected.**
- *Persisted pick log* — survives reloads and gives "picks today" analytics, but adds a table/write path and makes the core stateful.
- *Pure uniform random each time* — simplest, but teachers dislike "not fair" repeats — the whole point was avoiding them.

### 6.3 Group formation: by size, even, optional gender balance
**Choice.** Teacher picks **group size**; members distributed so groups differ by ≤1 (no lone straggler). **Optional gender balancing** stratifies students into `{Female, Male, null}` buckets and round-robin/snake-deals each bucket across groups so every category — *including unrecorded* — spreads evenly. One pure, edge-case-rich function.

**Reasoning.** Owner-directed feature. The generalized stratified approach handles the null/unrecorded bucket cleanly rather than hardcoding "boys/girls."

**Note on the `Gender` field (§7.2).** Modeled as a **nullable enum `{Female, Male}`** per owner decision — a strict binary. This adds PII about minors (cutting against §7.1's minimal-PII stance), so the field is **optional**. The balancer treats whatever values are present (binary + null) as strata, so it still generalizes mechanically even though the enum itself is binary.

**Rejected (for the algorithm).** *By number-of-groups mode* and *single "groups of N, smaller last group"* — the former was dropped to keep one input mode; the latter produces the disliked straggler group.

**Rejected (for the gender model).** *Nullable `{Female, Male, Other}`* (more inclusive, generalizes better) and *free-text* (unusable for an even-distribution algorithm) — owner chose strict binary.

---

## 7. Data lifecycle

### 7.1 Soft-delete + minimal PII + a real purge path
**Choice.** Students and classes carry `DeletedAt`/`IsArchived` (soft delete via an **EF global query filter** — the one place a query filter is used). Store **minimal PII** (display/first name only; no student emails or DOB). Provide an explicit **hard-purge** that **anonymizes** (not orphans) ledger rows for genuine erasure.

**Reasoning.** Removing a student preserves ledger integrity and allows undo; classes archive at year-end without cascading away history; the small PII surface and a real erasure path are a defensible "I thought about minors' data" story.

**Rejected.**
- *Hard delete with cascade* — clean erasure by default, but unrecoverable mistakes and lost reporting.
- *No delete/archive in v1* — a classroom app that can't remove a student or close a year feels incomplete; retrofitting soft-delete filters later is fiddly.

### 7.2 Gender field
See §6.3 — added for gender-balanced grouping; nullable binary enum; optional to respect minimal-PII.

---

## 8. Architecture, frontend & delivery

### 8.1 Backend: vertical slice, no MediatR
**Choice.** Organize by feature (`Features/Points`, `Features/Store`, …); endpoints call handlers directly. A shared Domain layer holds entities + pure logic; EF Core + Postgres in Infrastructure.

**Reasoning.** "Right altitude" for this size — modern, navigable, every layer defensible. Avoids both extremes.

**Rejected.**
- *Classic N-layer* — universally understood but boilerplate-heavy and the "default" everyone shows.
- *Clean Architecture + MediatR + CQRS* — signals big-pattern familiarity, but risks looking over-engineered for a classroom CRUD app; more ceremony, slower.

### 8.2 Frontend: Next.js, client-side, static-exported
**Choice.** Keep **Next.js** (App Router + TS) as a **client-side SPA shell**; **TanStack Query** is the primary data layer (cache invalidation in place of realtime; optimistic updates for points/store). Build as **static export**, served by **Nginx** — **no Node runtime in production**. Production stack = Nginx + API + Postgres.

**Reasoning.** Next.js was kept (over Blazor) for full-stack React/TS signal and marketability, with a mandate to make the API exemplary. Because the app is an interactive behind-login dashboard, SSR buys little, so client-side TanStack Query is the honest fit — and since no server features are used, static export drops a whole running service.

**Rejected.**
- *Blazor* — all-C# and shows .NET breadth, but less marketable and loses the "I can do React" signal.
- *Server Components + Server Actions* — leans toward the BFF model we didn't pick, and is awkward for the highly-interactive kiosk/board.
- *Plain Vite SPA* — lighter, but Next.js is more recognizable on a CV and was a deliberate choice.
- *Run the Next.js Node server* — keeps SSR options open, but adds a process to run/secure for zero current benefit.

### 8.3 API maturity, migrations, CI/CD
**Choices & reasoning.**
- **ProblemDetails (RFC 9457)** for all errors incl. validation — clearest "designs APIs well" signal; pairs with Scalar. (Health checks, `/v1` versioning, correlation IDs were considered and **deferred** as stretch.)
- **Minimal APIs + `MapGroup`** (pairs with vertical slice), **FluentValidation**, **hand-mapped DTOs** (no AutoMapper), **Scalar** docs, structured JSON logging via `ILogger`.
- **EF migrations via a migration bundle as an explicit deploy step** — explicit, reviewable, no startup race, gateable/rollbackable. Rejected: *auto-migrate on startup* (couples migrate to boot, races at >1 instance, crash-loops on a bad migration) and *manual* (not automated, undercuts the CI/CD story).
- **GitHub Actions** build/test (Testcontainers runs on hosted runners) → **GHCR** → SSH `docker compose pull && up`.

### 8.4 Durability: scripted off-box backups
**Choice.** Nightly `pg_dump` (compressed, rotated) **plus** a copy of the DataProtection key directory, pushed **off-box** to S3-compatible storage; a documented restore procedure in the repo.

**Reasoning.** Recoverable even if the VPS dies; strong ops-maturity signal for modest effort. Backs up the two things that hurt to lose: the DB and the keys (§5.5).

**Rejected.**
- *Local-only dumps* — protects against fat-finger/bad-migration but a dead VPS loses everything including backups.
- *Hetzner whole-VM snapshots* — easy and captures DB+keys together, but coarser, costs extra, and shows less of our own ops thinking.

### 8.5 Primary keys: GUID v7
**Choice.** `Guid.CreateVersion7()` for entity keys.

**Reasoning.** Non-guessable (no `/students/1,2,3…` enumeration; doesn't leak counts) as defense-in-depth alongside the authz handlers, while v7's time-ordering stays index-friendly in Postgres (unlike random v4). Safe to expose in URLs.

**Rejected.**
- *Sequential int/bigint* — smallest/fastest and authz already covers IDOR, but sequential ids in URLs leak counts and invite enumeration.
- *int internally + public slug* — best of both technically, but two identifiers per entity to maintain — more than a single-user app warrants.

---

## 9. Scope, scale & engagement

### 9.1 Single device, no realtime
**Constraint (owner-stated).** The app runs only on the teacher's laptop; students have no device showing the app. Therefore **no SignalR, no polling** — the SPA simply invalidates/refetches after its own mutations. Scale concerns evaporate (≈one concurrent user); the engineering rigor is a portfolio signal, to be stated in the README.

### 9.2 Demo integrity: writable + scheduled reset
**Choice.** The shared demo account is fully **writable** (interactivity is the pitch), and a scheduled job **re-seeds** it to a rich, populated state (full class, varied histories, owned avatars).

**Reasoning.** Reviewers always land on something impressive and can play freely.

**Rejected.**
- *Read-only demo* — always pristine but neuters the core experience (can't try awarding/shopping).
- *Per-visitor ephemeral sandbox* — best UX, but reintroduces multi-tenant provisioning/cleanup we simplified away.

### 9.3 Reporting: lightweight in v1
**Choice.** Leaderboard + per-student timeline + per-class behavior breakdown over a date range — straightforward EF aggregations over the ledger.

**Rejected.** *Defer entirely* (leaves an easy, high-value, low-cost feature on the table) and *rich dashboard with drilldowns/exports* (scope-heavy, pulls focus from backend craft).

### 9.4 Gamification: stay lean
**Choice.** **No** badges/levels/streaks in v1 — the leaderboard + avatar store already form a complete earn→spend→show-off loop. An achievements slice is the designated **first stretch** (additive; no schema reshape).

**Reasoning.** Protects the backend-craftsmanship focus and keeps the tracer-bullet path short.

### 9.5 Visual direction: polished playful-but-clean
**Choice.** Friendly, colorful, intentional — rounded shapes, a confident accent palette, generous spacing; the DiceBear avatars supply the "fun" while layout/typography/hierarchy stay crisp. Build a **small custom design system** (tokens + a few components), **not** stock component-library styling.

**Rejected.**
- *Clean & minimal* — safe but generic for a playful classroom product; easy to mistake for a template.
- *Bold & gamified* — memorable but risks looking gimmicky and is more frontend effort that distracts from the backend point.

---

## 10. Build sequencing

**Choice.** A **tracer bullet first**: log in as a seeded teacher → create a class → add students → render the roster — established end-to-end before feature depth. (Per owner direction, the **deployment** half is deferred to the very end, once the app is production-ready; the local skeleton + CI come first.)

**Reasoning.** De-risks the rails; once they exist, every feature is a low-risk additive vertical slice.

**Rejected.**
- *Domain-first (features locally, infra last)* — more immediately fun, but infra surprises hit late.
- *Schema-first (model the whole DB up front)* — satisfying, but big up-front schemas churn once real features push back.

Suggested order after the skeleton: behaviors + points ledger → leaderboard/wallet → reporting → store + avatar + kiosk → tools (picker, grouping, timer) → demo seeding/reset → **deploy** → backups. See [docs/issues/](./issues/) for the sliced tickets and their dependency graph.
