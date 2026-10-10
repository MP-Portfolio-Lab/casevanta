# ADR-0003: Keycloak replaces the inherited OpenIddict server

- Status: Accepted
- Date: 2026-10-10

## Context

The imported sample ships its own OpenIddict server with ASP.NET Core Identity pages, SQLite and two
dependencies with high-severity advisories. Casevanta needs separate workforce and customer
identities, standard administration and no identity code of its own.

## Decision

- Keycloak (Aspire `AddKeycloak`, HTTPS with the development certificate) is the identity provider.
- One realm per audience: `casevanta-workforce` now, a customer realm with the customer application.
- Realms are code: `infra/keycloak/realms/*.json` is imported on every start, with no data volume.
- The BFF's OIDC client (`staff-web`) is confidential and uses PKCE; its secret is generated per
  machine by an AppHost parameter and injected into Keycloak through a realm placeholder.
- The .NET OIDC handler uses pushed authorization requests automatically because Keycloak offers
  them.
- The OpenIddict server was deleted only after suites A, B and C passed on Keycloak.

## Consequences

- No identity code to maintain; Keycloak's account policies, brute-force protection and sessions
  apply.
- `Aspire.Hosting.Keycloak` is still a preview package (threat model G14); suites B and C are the
  regression net for each update.
- Keycloak's own telemetry does not reach the dashboard yet; the BFF side of every exchange is traced.
- Production needs a different way to provision realms (the import is development-only).

## Alternatives considered

- **Keep OpenIddict.** Rejected: identity server code and its vulnerabilities would be ours to own.
- **A hosted identity provider.** Deferred: it would make the demo depend on an external account.
