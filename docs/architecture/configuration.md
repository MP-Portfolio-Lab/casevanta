# Configuration reference

| | |
|---|---|
| Updated | 2026-10-10 |
| Scope | Every setting a developer or deployment can change, where it lives, and its value per environment |

ASP.NET Core reads settings from `appsettings.json`, then `appsettings.{Environment}.json`, user
secrets (Development), environment variables and command-line arguments, later sources winning.
Environment variables use `__` between sections, for example `OpenIDConnectSettings__Authority`.
`.env.example` lists the overridable values; nothing loads it automatically.

## BFF (`backend/src/Casevanta.Api`)

| Setting | Purpose | Development | Test (suite A) | Production |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Selects settings, dev proxy and diagnostics | `Development` (launch profile) | `Test` | `Production` |
| Kestrel URL | Public origin of the BFF | `https://localhost:5001` (launch profile; Keycloak redirect URIs depend on it) | in-memory host `https://staff.casevanta.test` | set by the host |
| `OpenIDConnectSettings:Authority` | Identity provider the BFF trusts; also allowed in CSP `form-action` | `https://localhost:44318` (inherited OpenIddict server) | same, with a static discovery document | HTTPS authority of the deployed identity provider |
| `OpenIDConnectSettings:ClientId` | OIDC client of the BFF | `oidc-pkce-confidential` | same | per environment |
| `OpenIDConnectSettings:ClientSecret` | Client secret for the token endpoint | sample value in `appsettings.json` (gap G8, replaced in the Keycloak step) | same | secret store only |
| `UiDevServerUrl` | Where `_Host.cshtml` fetches `index.html` in Development | `http://localhost:4201` (injected by the AppHost; `appsettings.Development.json` holds the same value for runs without Aspire) | not used | not used |
| `ReverseProxy:*` | YARP routes to the Angular dev server (scripts, styles, `@vite`, `@fs`, HMR websocket, favicon) | cluster `staff-web`, destination `dev-server` at `http://localhost:4201/` | not mapped | not mapped |

Outside Development, `Pages/_Host.cshtml` reads `wwwroot/index.html` from the working directory.
Packaging the Angular build (`dist/apps/staff-web/browser`) into that folder or a container image is
part of the deployment work and is not automated yet. Keeping build output out of the API project
also stops a stale local bundle from shadowing the dev server in Development.

## Local orchestration (Aspire)

| Setting | Purpose | Value |
|---|---|---|
| Resources | What `scripts/dev.sh` starts | `postgres` with database `casevanta`, `identity-provider` (44318), `staff-web` (4201, after `pnpm install --frozen-lockfile`), `api` (5001) |
| `Frontend:Enabled` | AppHost switch for the Angular dev server | `true`; suite C passes `false` so it needs neither Node.js nor pnpm |
| Injected into `api` | Values the AppHost provides at start | `ConnectionStrings__casevanta`, `UiDevServerUrl`, the YARP destination address, `OTEL_EXPORTER_OTLP_ENDPOINT` |
| Health endpoints | Readiness and liveness | `/health` and `/alive`, mapped in Development only |
| Dashboard | Logs, traces, metrics, resource state | `https://localhost:17032` (AppHost launch profile; the login token is printed on start) |
| `AspireCliInvocationMode` | How AppHost builds find the Aspire CLI bundle (orchestrator and dashboard) | unset locally (the installed CLI on `PATH`); `dnxpinned` in CI (`dnx aspire.cli@<SDK version>`) |

## Frontend workspace

| Setting | Where | Value |
|---|---|---|
| Node.js | `.nvmrc`, `package.json` `engines` | 22 (22.22.3+), 24.15+ or 26+ (Angular 22 support range) |
| pnpm | `package.json` `packageManager` | 12.10.1; build scripts only for the packages listed in `pnpm-workspace.yaml` |
| TypeScript | `package.json` | `~6.0.3`; Angular 22 supports `>=6.0 <6.1` only |
| Dev server | `apps/staff-web/project.json` (`serve`) | `http://localhost:4201` |
| Production build output | `apps/staff-web/project.json` (`build`) | `dist/apps/staff-web` |
| CSP nonce | `apps/staff-web/src/index.html` | `**PLACEHOLDER_NONCE_SERVER**`, replaced per request by the BFF |
| UI components | `components.json`, `libs/shared/ui` | spartan-ng helm components owned in the repository (`@spartan-ng/helm/*`) |

## Browser journeys (`apps/staff-web-e2e`)

| Setting | Purpose | Default |
|---|---|---|
| `E2E_IDP` | Identity provider adapter the journeys use | `openiddict` |
| `CI` | Disables reuse of already-running servers and enables one retry | unset locally |

The journeys start the identity provider, the dev server and the BFF themselves
(`playwright.config.mts`), or reuse ones that are already running locally. The inherited identity
provider stores its data in `tmp/e2e/identity-provider.sqlite`, so every machine starts clean.
