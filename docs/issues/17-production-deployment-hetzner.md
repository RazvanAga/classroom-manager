# Slice 16 — Production deployment to Hetzner (CD)

**Type:** HITL

## Parent

#1 — PRD: Classroom Manager

## What to build

Make the app production-ready and deploy it to the Hetzner VPS via the full CD pipeline. **This is done only once the app is production-ready** (the feature slices are complete) — it is the second half of the original walking-skeleton slice, deliberately deferred to the end.

Scope:
- Multi-stage Dockerfile for the API (SDK → aspnet runtime, non-root user).
- Production `docker-compose` = **Nginx + API + Postgres**; frontend served as **static-export assets by Nginx**; API and frontend under a **single domain** (`/api` proxied).
- **GitHub Actions CD**: build images → push to **GHCR** → SSH to the VPS → `docker compose pull && up`.
- **EF migrations** applied via a **migration bundle** as an explicit deploy step (not on app startup).
- **DataProtection keys** persisted to a mounted volume + fixed application name (auth survives redeploys).
- Secrets injected from a root-only `.env` on the VPS (GitHub Actions secrets), never baked into images.

HITL: requires Hetzner/DNS/secrets decisions and server access.

## Acceptance criteria

- [ ] CI builds and pushes images to GHCR; CD deploys to Hetzner over SSH.
- [ ] Migrations are applied via the migration-bundle step (not on app startup).
- [ ] DataProtection keys persist across redeploys (sessions survive a deploy).
- [ ] The app is reachable at a single domain (frontend + `/api`) behind Nginx.
- [ ] Secrets are injected from the VPS `.env`, not baked into images.

## Blocked by

- All feature slices (#2–#16) — deploy when production-ready.

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
