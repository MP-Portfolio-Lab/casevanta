# ADR-0004: Characterisation tests before every migration step

- Status: Accepted
- Date: 2026-10-10

## Context

The cleaning phase changed authentication, BFF security, the Angular workspace and the local
orchestration at once. Without a safety net, any of these could silently break login, logout,
antiforgery or the Content Security Policy.

## Decision

Before changing the inherited code, tests recorded how it behaved. Every later step followed the loop:
extend or adapt tests, make one change, run everything, fix, commit. Five layers, each with stable IDs
that the threat model cites:

| Layer | What it proves | Needs |
|---|---|---|
| A, API and security (`WebApplicationFactory`) | BFF behaviour in memory, with a static OIDC configuration and forged session cookies | .NET |
| Architecture rules (NetArchTest) | Dependency rules of ADR-0005 | .NET |
| Frontend unit (Vitest) | Interceptor, cookie reader, shell and page contracts | Node.js |
| C, distributed (`Aspire.Hosting.Testing`) | The real topology with Keycloak and PostgreSQL | Docker |
| B, browser journeys (Playwright) | Sign-in, protected calls, roles, sign-out, CSP violations | Docker, Node.js |

Rules: the API stays testable without Aspire; browser journeys are never cached by Nx (they depend on
.NET code Nx does not track); known gaps are documented by tests until fixed.

## Consequences

- Every migration step ended green; regressions were caught before commits (for example the
  protocol-relative open redirect and the CSP origin of `form-action`).
- Suite B and suite C need Docker and take about a minute each; they run in their own CI jobs.
- New business modules follow conventional test-first development: unit tests for domain rules,
  integration tests for persistence and messaging, journeys for critical workflows.
