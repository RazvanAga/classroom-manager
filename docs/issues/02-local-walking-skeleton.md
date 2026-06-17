# Slice 1 — Local walking skeleton (seeded login)

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

The end-to-end local walking skeleton that proves every layer connects, with **no deployment**. A teacher seeded into the database can log in through the Next.js frontend and see "logged in as &lt;name&gt;", backed by the real API and a real Postgres. This slice establishes the solution structure and the two reusable test seams.

Scope:
- Backend in **vertical-slice** layout (feature slices, a Domain layer, an Infrastructure layer with EF Core + PostgreSQL), .NET 10, Minimal APIs grouped via `MapGroup`, hand-mapped DTOs.
- **ASP.NET Core Identity** with one **seeded teacher** (seed/migration or a small CLI). No public registration, no SMTP.
- **Cookie auth** (HttpOnly + SameSite) with **antiforgery** on mutations; `login`, `logout`, and `me` (current teacher) endpoints.
- **ProblemDetails (RFC 9457)** for all errors including validation (FluentValidation). **Scalar** over the OpenAPI doc.
- **Next.js (App Router + TS)** configured for static export; a login page that authenticates and shows the logged-in teacher's name; **TanStack Query** as the data layer.
- Local dev runs against Postgres (e.g. a dev docker-compose for the DB only).
- **GitHub Actions CI** runs BOTH seams on push: xUnit unit tests and integration tests via **WebApplicationFactory + Testcontainers Postgres**.

Out of scope (these belong to the production-deployment slice): Hetzner/prod deploy, GHCR, prod Nginx serving, DataProtection key persistence.

## Acceptance criteria

- [ ] A seeded teacher can log in via the frontend and see their name; logout works.
- [ ] Unauthenticated requests to protected endpoints are rejected; mutations require antiforgery.
- [ ] All error responses use ProblemDetails (RFC 9457), including validation failures.
- [ ] Scalar API docs are served over the OpenAPI document.
- [ ] CI runs unit tests AND Testcontainers-backed integration tests (WebApplicationFactory) and passes.
- [ ] The integration-test harness (WebApplicationFactory + Testcontainers Postgres fixture) is structured for reuse by later slices.
- [ ] The app runs locally end-to-end (frontend → API → Postgres).

## Blocked by

None - can start immediately.

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
