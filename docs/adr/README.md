# Architecture decision records

Each record states one decision, its context and its consequences. Records are not edited after
acceptance; a later record supersedes an earlier one instead.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-bootstrap-from-bff-sample.md) | Start from a sanitised import of damienbod's BFF sample with fresh history | Accepted |
| [0002](0002-aspire-first-local-orchestration.md) | Aspire AppHost as the only local startup path | Accepted |
| [0003](0003-keycloak-for-workforce-identity.md) | Keycloak replaces the inherited OpenIddict server | Accepted |
| [0004](0004-tests-first-migration.md) | Characterisation tests before every migration step | Accepted |
| [0005](0005-module-boundaries.md) | Modular monolith with enforced module boundaries | Accepted |
| [0006](0006-identity-versus-authorization-and-tenancy.md) | Keycloak authenticates; Casevanta authorises, per tenant | Accepted |
| [0007](0007-api-conventions.md) | API conventions: problem details, OpenAPI, validation, URLs | Accepted |
| [0008](0008-session-and-data-protection.md) | Server-side cookie session and Data Protection key storage | Accepted |
| [0009](0009-development-data-lifecycle.md) | Disposable, synthetic development data | Accepted |
| [0010](0010-public-origin-and-forwarded-headers.md) | Configured public origin; forwarded headers only behind a known proxy | Accepted |
| [0011](0011-ui-component-strategy.md) | spartan-ng components owned in the repository, on Tailwind CSS | Accepted |
| [0012](0012-repository-layout.md) | One folder per stack; the Nx workspace lives in `frontend/` | Accepted |
