# Classroom Manager

A classroom management app for teachers — track classes and students, award behavior points, and turn those points into a reward loop (customizable avatars bought from a store), alongside everyday tools: a fair random student picker, a random group maker, and an activity timer.

Built as a **portfolio project targeting .NET roles**. It runs as a single-teacher tool, but is engineered as if it mattered at scale — clean architecture, real authorization, transactional integrity, a full test pyramid, CI, and production ops. That rigor is a **deliberate demonstration**, not a response to load: the app is designed for one concurrent user on a teacher's laptop. See [docs/design.md](docs/design.md) for the reasoning behind every decision (including the alternatives that were rejected and why).

> **Status:** 🚧 In active development. The design is complete and sliced into [tickets](docs/issues/); implementation starts with the [walking-skeleton tracer bullet (#2)](https://github.com/RazvanAga/classroom-manager/issues/2). A live demo URL will appear here once deployed.

---

## Features

- **Multiple classes & co-teaching** — a teacher owns or collaborates on many classes (many-to-many with an Owner role and resource-based authorization).
- **Behavior points** — award or deduct points to one student or the whole class against a per-class catalog of named behaviors. Every change is an immutable ledger entry, so history, undo, and reporting come for free.
- **Two views from one ledger** — a spendable **wallet** and a **lifetime-earned** leaderboard score that shopping never reduces.
- **Customizable avatars** — each student has a layered avatar (DiceBear); students spend points in a **store** to buy and equip options.
- **Kiosk mode** — the teacher hands over the laptop; a student taps their name (no login) and shops/equips within a reduced-scope session that can't touch teacher functions.
- **Classroom tools** — a fair student picker (no repeats until everyone's been called), a random group maker (pick group size, even distribution, optional gender balancing), and an activity timer.
- **Lightweight reporting** — leaderboard, per-student point timeline, and per-class behavior breakdowns.

## Tech stack

| Layer | Choice |
|-------|--------|
| Backend | C# / ASP.NET Core (.NET 10), vertical-slice architecture, Minimal APIs |
| ORM / DB | EF Core + PostgreSQL |
| Auth | ASP.NET Core Identity (cookie auth, same-domain) |
| API docs | Scalar over OpenAPI; ProblemDetails (RFC 9457) errors |
| Frontend | Next.js (App Router + TypeScript), TanStack Query, static export |
| Testing | xUnit — unit tests on pure domain logic + integration tests via `WebApplicationFactory` + Testcontainers (real Postgres) |
| CI/CD | GitHub Actions → GHCR → Hetzner VPS |
| Hosting | Docker Compose (Nginx + API + Postgres) on a Hetzner VPS |
| Logging | Built-in `ILogger`, structured JSON to stdout |

## Architecture highlights

A few decisions worth calling out (full rationale in [docs/design.md](docs/design.md)):

- **Append-only points ledger** — balance is the sum of immutable transactions; undo is a soft-void that keeps both the wallet and the leaderboard correct without compensating entries.
- **Transactional purchases** — a serializable transaction (re-sum → check → insert, retry on conflict) plus a `UNIQUE(StudentId, ItemId)` constraint prevents overspend and double-buy.
- **Resource-based authorization** — membership and Owner handlers enforce that a teacher can only touch their own classes; cross-tenant denial is covered by integration tests.
- **Reduced-scope kiosk principal** — kiosk mode mints a separate short-lived session, so a student holding the laptop genuinely cannot reach privileged endpoints (not just hidden UI).
- **GUID v7 primary keys**, **soft-delete via EF global query filter**, and **DataProtection keys persisted to a volume** so auth survives redeploys.

## Documentation

- [docs/PRD.md](docs/PRD.md) — product requirements (problem, solution, user stories, decisions). Mirrored to [issue #1](https://github.com/RazvanAga/classroom-manager/issues/1).
- [docs/design.md](docs/design.md) — design decisions and rejected alternatives (the "why").
- [docs/issues/](docs/issues/) — the work, sliced into independently-grabbable vertical slices ([#2–#18](https://github.com/RazvanAga/classroom-manager/issues)).

## Roadmap

Work is broken into vertical "tracer-bullet" slices, each demoable on its own. In dependency order: walking skeleton → classes & authorization → roster → behaviors → points ledger → undo → avatars → store → kiosk → tools (picker, groups, timer) → reporting → purge → demo account → **production deployment** → backups. See the [issue index](docs/issues/README.md) for the dependency graph.

## Local development

> Setup instructions (prerequisites, running the API + frontend + Postgres locally, running the tests) will be added with the [walking-skeleton slice (#2)](https://github.com/RazvanAga/classroom-manager/issues/2), where the project scaffolding and run commands are established.

## License

MIT — see [LICENSE](LICENSE).
