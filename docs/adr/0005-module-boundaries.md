# ADR-0005: Modular monolith with enforced module boundaries

- Status: Accepted
- Date: 2026-10-10

## Context

Casevanta's domain splits into applications, verification, documents and signing, inspections,
audits, remediation and reporting. They change at different speeds and some (documents, AI
processing) may become separate services. Starting with network services would add operational cost
before it is justified.

## Decision

- One deployable backend. Each business module is one project, `Casevanta.Modules.<Name>`, organised
  in vertical slices (`Features/<UseCase>`), with domain types where behaviour justifies them.
- A module's public surface is its `Contracts` namespace (requests, responses, integration events).
  Other modules may use only that.
- `Casevanta.Api` is the composition root and the BFF: it registers modules and maps their endpoints,
  but holds no business logic.
- One PostgreSQL database, one schema per module; a module owns its tables and writes, and never
  reads another module's tables directly.
- Enforcement: architecture tests (no module depends on `Casevanta.Api` or on another module's
  internals; the API never depends on Aspire hosting) and Nx tags on the frontend (`type:*` layering,
  `scope:staff`, `scope:customer`, `scope:shared`).

## Consequences

- A module can later move into its own service by replacing in-process contract calls with messages.
- Cross-module workflows need explicit contracts or events instead of shared tables.
- The rules exist before the first module, so they cannot be retrofitted around existing violations.
