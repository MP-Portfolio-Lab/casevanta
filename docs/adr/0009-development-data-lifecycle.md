# ADR-0009: Disposable, synthetic development data

- Status: Accepted
- Date: 2026-10-10

## Decision

- Development data is synthetic and disposable. No production data is ever copied into development,
  tests or the repository.
- Keycloak's realm import (`infra/keycloak/realms`) is the single source of development users,
  roles and clients; it is re-imported on every start.
- PostgreSQL runs without a volume, so every start gives an empty database.
- When the first module arrives:
  - each module owns EF Core migrations for its own schema, applied by a migration step in
    Development (and by the deployment pipeline elsewhere), never by the API at request time;
  - synthetic business data (customers, applications, cases) comes from a seed tool in
    `tools/seed`, built on the modules' contracts;
  - volumes may be added for convenience, and `scripts/reset-dev-data.sh` then removes them.
- `scripts/reset-dev-data.sh` returns the environment to a clean state; `--secrets` also
  regenerates the AppHost's generated parameters.

## Consequences

- Every developer and CI run starts from the same known state.
- Long-lived local data is not supported until volumes are added deliberately.
