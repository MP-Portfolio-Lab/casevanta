# ADR-0007: API conventions

- Status: Accepted
- Date: 2026-10-10

## Decision

- **Errors**: every `/api` error is RFC 9457 problem details (`application/problem+json`): unknown
  routes, 401, 403 and unhandled exceptions. Outside Development, responses never contain exception
  types, messages or stack traces (tests A14, A26, A27).
- **Status codes**: 400 malformed request; 401 no session; 403 session without permission; 404
  unknown resource (also for resources outside the caller's tenant); 409 concurrency conflict or
  invalid state transition; 422 business rule violation.
- **Validation**: data annotations with .NET 10's built-in validation (minimal APIs) and
  `[ApiController]` (controllers), both producing validation problem details. A validation library
  is added only when rules outgrow annotations.
- **Concurrency and retries**: updates carry a version (ETag / `If-Match` or a version field) and
  conflicting writes get 409; commands that create things accept an idempotency key.
- **OpenAPI**: `/openapi/v1.json` in Development only (A28, C07). The TypeScript client for the
  frontend will be generated from it into `libs/shared/api-client`.
- **URLs**: new routes are lowercase kebab-case under `/api/<module>/...`; inherited routes
  (`/api/User`, `/api/DirectApi`, `/api/Account/*`) move with the first module. URLs never carry
  secrets or personal data, because query strings reach telemetry (threat model G13).
- **Versioning**: none while the SPA is the only consumer; add a version segment when an external
  consumer appears.
