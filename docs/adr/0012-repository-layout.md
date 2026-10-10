# ADR-0012: One folder per stack; the Nx workspace lives in `frontend/`

- Status: Accepted
- Date: 2026-10-11

## Context

The bootstrap left the Nx workspace at the repository root. `apps/`, `libs/` and nine JavaScript
tooling files sat next to `backend/`, as if each were a peer of the whole .NET solution. A
reader could not tell which stack owned which folder, and frontend tooling dominated the root. The
plan adds a customer application, possibly a Python document or AI worker, and infrastructure as
code, so the layout needs a rule before they arrive.

## Decision

- Each stack lives in one top-level folder with its own toolchain root. `backend/` holds the .NET
  solution. `frontend/` holds the pnpm and Nx workspace: `package.json`, the lockfile, `nx.json`,
  `tsconfig.base.json`, the ESLint, PostCSS and spartan-ng configuration, `.nvmrc`, `apps/` and
  `libs/`. A later stack, such as a Python worker, gets its own folder in the same way.
- The repository root holds only what the stacks share: CI and Dependabot, git hooks
  (`lefthook.yml`), formatting rules (`.editorconfig`, `.prettierrc`, `.prettierignore`),
  `.gitignore`, the gitleaks configuration, `infra/`, `docs/`, `scripts/` and the licence files.
- `global.json` stays at the root. The `dotnet` host looks for it from the working directory upward,
  not from the project path, and it also selects Microsoft.Testing.Platform for xUnit v3. At the
  root it applies to every `dotnet` command in the repository.
- `.gitignore` stays a single root file. Its patterns are unanchored (`dist/`, `**/.nx/cache/`), so
  they also apply inside `frontend/`. Git, Nx and Tailwind CSS's class scanner all read it from
  there.
- Prettier formats the whole repository with the root configuration, as `nx format` did when Nx sat
  at the root. `pnpm format` and `pnpm format:check` in `frontend/`, the pre-commit hook and the
  `web` CI job all run it that way. `frontend/.prettierignore` only stops `nx format`, which runs
  Prettier from `frontend/`, from rewriting the lockfile.
- Scripts and CI run `dotnet` from the repository root and `pnpm` or `nx` in `frontend/`: through
  `pnpm -C frontend` in scripts and `defaults.run.working-directory` in CI.
- `.vscode/settings.json` is tracked. It points Nx Console, ESLint and Prettier at `frontend/`. Nx
  Console exports its workspace path as `NX_WORKSPACE_ROOT_PATH` into the editor's extension host.
  Every process an extension starts inherits it, and Nx prefers it to its own lookup.
- Paths in earlier records that start with `apps/` or `libs/` now start with `frontend/`. Those
  records are otherwise unchanged.

## Consequences

- Each stack can be read, built and updated on its own. A new stack adds a folder, not files at the
  root.
- No Nx project file changed, because every path in them is relative to the Nx workspace root.
- Commands need the right working directory. `pnpm` and `nx` fail at the repository root, which has
  no `package.json`, so the README and scripts use `pnpm -C frontend`.
- The browser journeys' Playwright configuration derives the repository root from its own location
  when it starts the AppHost, so a wrong `NX_WORKSPACE_ROOT_PATH` cannot move it.
- Dependabot's npm updates target `/frontend`.

## Alternatives considered

- **Keep Nx at the root and move only `apps/` and `libs/` into `frontend/`.** Rejected: every
  project, TypeScript and ESLint path would change, and the root would keep the frontend tooling
  that prompted this decision.
- **One folder per audience (`staff/`, `customer/`), each with its own frontend and backend.**
  Rejected: the backend is one modular monolith that serves both audiences (ADR-0005), and the
  frontends share libraries through one Nx workspace.
