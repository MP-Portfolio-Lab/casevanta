# ADR-0002: Aspire AppHost as the only local startup path

- Status: Accepted
- Date: 2026-10-10

## Context

The local topology already has five moving parts (BFF, Angular dev server, Keycloak, PostgreSQL and
telemetry) and will grow with workers and messaging. Developers, browser tests and CI all need to
start the same topology the same way.

## Decision

- `backend/src/Casevanta.AppHost` (Aspire 13.6) is the single way to start the system locally
  (`scripts/dev.sh`); there is no `compose.yaml`.
- Browser-facing endpoints keep fixed ports (BFF 5001, Keycloak 8080, dev server 4201) because
  redirect URIs and cookies depend on them; everything else uses Aspire's defaults.
- The AppHost uses the Aspire CLI bundle (orchestrator and dashboard): the installed CLI locally, the
  pinned NuGet package (`dnx aspire.cli@<SDK version>`) in CI, so no install script is piped to a
  shell.
- `Casevanta.ServiceDefaults` gives every .NET service OpenTelemetry, health checks, service
  discovery and HTTP resilience.
- The API never depends on Aspire hosting (architecture rule AR01); it stays runnable and testable
  in memory (suite A).

## Consequences

- One command starts everything, with logs, traces and resource health in the dashboard.
- Distributed tests (suite C) and browser journeys (suite B) start the real topology through the
  same AppHost.
- Docker is a prerequisite for local development and for two CI jobs.
- Deployment artifacts can later be produced with `aspire publish`; this is not decided yet.

## Alternatives considered

- **Docker Compose plus `dotnet run` and `pnpm start`.** Rejected: three startup paths to keep in
  sync, no telemetry dashboard, and no first-class test integration.
