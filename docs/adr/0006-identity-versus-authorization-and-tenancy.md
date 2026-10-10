# ADR-0006: Keycloak authenticates; Casevanta authorises, per tenant

- Status: Accepted (model decided; built with the first module)
- Date: 2026-10-10

## Context

Keycloak issues coarse realm roles (`employee`, `verifier`, `inspector`, `auditor`,
`administrator`). The product needs finer rules: an auditor must not see every organisation's cases,
a reviewer must not approve their own work, and records belong to an organisation.

## Decision

- **Identity** is Keycloak's job: who the user is (`sub` and issuer) and coarse roles.
- **Authorisation** is Casevanta's job, in the backend, for every request:
  - a staff identity record keyed by issuer and `sub`, with organisation (tenant) membership and team
    scope, owned by the customers-and-organisation-access module;
  - resource checks on ownership, assignment and process state;
  - separation of duties: the author of a decision cannot review or inspect it; an auditor cannot
    rewrite the original decision;
  - administrative access does not imply access to customer documents.
- Endpoints refer to named policies (`AuthorizationPolicies`), never to role strings.
- Every business record carries a tenant identifier; isolation tests run a second tenant.
- Every consequential decision records actor, time, reason, evidence version and, where relevant, the
  policy or model version.
- Background workers apply the same tenant and evidence scope as the API.

## Consequences

- Role changes in Keycloak alone cannot grant access to another organisation's data.
- Authorisation logic lives in testable code next to the modules, not in identity-provider
  configuration.
- A relationship-based authorisation engine stays an option if delegation and sharing become hard to
  express.
