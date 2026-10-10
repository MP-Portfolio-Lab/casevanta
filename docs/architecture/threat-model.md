# Threat model: browser-facing BFF

| | |
|---|---|
| Status | Written before any change to the inherited BFF; updated after each migration step |
| Updated | 2026-10-11 |
| Scope | The ASP.NET Core BFF (`backend/src/Casevanta.Api`), the staff SPA it serves, and the identity provider it trusts (Keycloak, realm `casevanta-workforce`; the inherited OpenIddict server until 2026-10-10) |
| Method | Data flows and trust boundaries, then threats per boundary (STRIDE category in brackets). Every mitigation names the automated test that keeps it true. |

Test IDs refer to `backend/tests/Casevanta.Api.Tests` (A, in-memory API and header tests),
`frontend/apps/staff-web-e2e` (B, browser journeys) and `backend/tests/Casevanta.AppHost.Tests` (C,
the real topology with Keycloak). Update this document whenever a test ID, a mitigation or a gap changes.

## System and trust boundaries

```mermaid
flowchart LR
    subgraph browser["Browser (untrusted)"]
        spa["Staff SPA"]
    end
    subgraph server["BFF host (trusted)"]
        bff["BFF: cookie session, antiforgery, CSP, API"]
        dev["Angular dev server (Development only)"]
    end
    idp["Identity provider"]

    spa -- "TB1: same-origin HTTPS, session cookie + XSRF header" --> bff
    browser -- "TB2: front channel (authorize, login, end session)" --> idp
    bff -- "TB3: back channel (discovery, token, userinfo)" --> idp
    bff -- "TB4: YARP proxy + index.html fetch" --> dev
```

| Boundary | Crossing | Notes |
|---|---|---|
| TB1 | Browser ↔ BFF | The SPA talks only to its own origin. Tokens never cross this boundary; the browser holds an encrypted, HttpOnly session cookie. |
| TB2 | Browser ↔ identity provider | Authorization request, login and consent pages, form_post response, end-session redirect. |
| TB3 | BFF ↔ identity provider | Server-to-server: discovery, code redemption with the client secret, userinfo. |
| TB4 | BFF ↔ dev server | Development only. Mapped only when the environment is Development. |
| TB5 | Repository and CI ↔ public | The repository is public; anything committed is published. |

## Assets

| Asset | Where it lives | Why it matters |
|---|---|---|
| Session cookie `.AspNetCore.Cookies` | Browser, encrypted with ASP.NET Core Data Protection | Contains the authentication ticket and, because `SaveTokens = true`, the ID, access and refresh tokens. |
| Antiforgery pair `__Host-X-XSRF-TOKEN` + `XSRF-RequestToken` | Browser | Proves a state-changing request came from the SPA. |
| OIDC client secret | BFF configuration | Authenticates the BFF at the token endpoint. |
| Data Protection keys | BFF host (user profile by default) | Decrypt every session cookie. |
| User identity and claims | Session ticket, `/api/User` | Personal data; drives authorization. |
| Business records and evidence | Future modules and database | Out of scope until the first module exists. |

## Threats, mitigations and tests

| ID | Threat | Mitigation in place | Verified by |
|---|---|---|---|
| T1 | [I/E] Cross-site scripting steals tokens or rides the session | Tokens stay server-side; session cookie is HttpOnly; strict CSP with a fresh per-request nonce injected into the host page; Angular template sanitisation | A05, A06, A07, A17, B05, B08 |
| T2 | [T] Cross-site request forgery against the API | Session cookie SameSite=Lax; double-submit antiforgery (`__Host-` HttpOnly cookie + `X-XSRF-TOKEN` header) bound to the signed-in user; global `AutoValidateAntiforgeryToken`; the data endpoint validates even GET; the SPA sends the token only to same-origin `/api/` URLs; logout requires the token too | A06, A09, A10, A12, A19, B06, staff-web unit tests (antiforgery interceptor) |
| T3 | [S] Login CSRF, authorization-code injection or replay | Authorization code flow with PKCE (S256), `state` and `nonce`, HttpOnly + Secure correlation and nonce cookies; pushed authorization requests keep the parameters off the front channel; redirect URIs are pinned by configuration and registered exactly in Keycloak | A04, A22, B05, C05 |
| T4 | [S] Open redirect through `returnUrl` after login | Absolute http(s) URLs are reduced to their path; relative ones are anchored at `/`; protocol-relative, backslash and control-character forms fall back to `/` | A15, A20 |
| T5 | [T] Clickjacking | `X-Frame-Options: DENY` and `frame-ancestors 'none'` | A07, A17 |
| T6 | [S] Session hijacking, fixation or an overlong session | Secure (always), HttpOnly, SameSite=Lax session cookie issued only after a completed OIDC sign-in; 8-hour sliding lifetime; Keycloak SSO idle 30 minutes, maximum 10 hours | A25, B05 |
| T7 | [E] Unauthenticated or unauthorised API access | `[Authorize]` on protected endpoints; named policies over Keycloak realm roles (flat `roles` claim); `/api` requests get 401 or 403 instead of a login redirect; unknown `/api` routes get 404 | A01, A02, A03, A08, A18, A23, A24, B02, B09, B10, C06 |
| T8 | [S] Logout leaves a live session at the BFF or identity provider | Logout deletes the session cookie and redirects to Keycloak's end-session endpoint with `id_token_hint` and a registered `post_logout_redirect_uri`; the next sign-in asks for credentials again | A11, A22, B07 |
| T9 | [I] Information disclosure through errors, headers or routing | No `Server` header; developer exception page, identity-model PII logging and health endpoints only in Development; unknown API routes return 404, client routes return the host page | A03, A16, A21, B03, B04 |
| T10 | [I/T] Transport downgrade or mixed content | HTTPS redirection; HSTS for one year (with `preload` on `/api`) outside Development; `block-all-mixed-content`; OIDC metadata only over HTTPS in every environment (the development Keycloak serves HTTPS with the development certificate) | A07, A17, C04 |
| T11 | [I] Secrets published through the public repository | Sanitised import (no private key, no user database); the OIDC client secret is generated per machine and never committed; development users are labelled as such; gitleaks over the full history in CI and on every commit; GitHub secret scanning with push protection | CI job `secrets`, pre-commit hook |
| T12 | [E] The development proxy exposed outside Development | YARP routes are mapped only in Development | Inspection (Program.cs); A16 (Test environment serves the host page, not the dev server) |
| T13 | [I] Cross-origin reads and side channels | COOP, COEP and CORP set to same-origin / require-corp; no CORS policy, so no cross-origin API access | A07, A17 |
| T14 | [All] Vulnerable dependencies | Dependabot alerts and security updates; CodeQL default setup | GitHub settings (`scripts/github-protect.sh`) |
| T15 | [I] Telemetry leaks secrets or personal data | Tokens stay in the server-side session and are never logged; `form_post` keeps the authorization code out of URLs; identity-model PII logging only in Development; health probes excluded from traces; telemetry goes only to the OTLP endpoint the AppHost configures | Observability checklist (`docs/architecture/overview.md`), A21 |

## Known gaps

Each gap is either fixed in a planned step or accepted with a reason. A test marked "known gap"
documents current behaviour and must change when the gap closes.

| ID | Gap | Threat | Plan |
|---|---|---|---|
| G1 | ~~Protocol-relative return URLs (`//evil.example`) pass the open-redirect guard~~ | T4 | Fixed 2026-10-10: only site-relative paths are accepted, everything else falls back to `/` (A20) |
| G2 | ~~Logout ignores antiforgery (`[IgnoreAntiforgeryToken]`), so another site can force a logout~~ | T2 | Closed 2026-10-10: logout requires the antiforgery token (A12) |
| G3 | Tickets are self-contained, so a copied cookie stays valid until it expires (now at most 8 hours of inactivity), even after logout; tokens are not refreshed | T6 | Lifetime bounded 2026-10-10 (A25). Server-side ticket store and token refresh: ADR-0008 |
| G4 | ~~`IdentityModelEventSource.ShowPII = true` in every environment (BFF and IdentityProvider)~~ | T9 | Closed 2026-10-10: Development only in the BFF (A21); the IdentityProvider was removed |
| G5 | CSP keeps `'unsafe-inline'` next to the nonce (ignored by nonce-aware browsers) and `style-src 'unsafe-inline'` | T1 | Re-evaluated 2026-10-10 with the Angular 22 and spartan-ng UI: B08 passes, but only in Development, where `style-src` allows `'self'` and `'unsafe-inline'`. The production policy (nonce-only styles) has no browser test yet. Add one, then remove the fallback together with violation reporting |
| G6 | Authorization is coarse: realm roles only, no organisation, team, ownership or separation-of-duty rules yet | T7 | First policy in place 2026-10-10 (A23); the resource-level model is decided in ADR-0006 and built with the first module |
| G7 | ~~The IdentityProvider depends on packages with known high-severity advisories (`SQLitePCLRaw.lib.e_sqlite3` 2.1.11, `Microsoft.OpenApi` 2.4.1)~~ | T14 | Closed 2026-10-10: the IdentityProvider was removed |
| G8 | ~~A sample OIDC client secret is committed in `appsettings.json` and the IdentityProvider seeding~~ | T11 | Closed 2026-10-10: generated per machine (AppHost parameter `staff-web-client-secret`) and injected into Keycloak and the BFF (C05) |
| G9 | Data Protection keys use the default per-user location; containers would lose them on restart | T6 | Application name pinned 2026-10-10 (A25); key storage for deployments in ADR-0008 |
| G10 | ~~The development identity provider would run over plain HTTP~~ | T10 | Closed 2026-10-10: the development Keycloak serves HTTPS with the development certificate, and the BFF requires HTTPS metadata everywhere |
| G11 | No back-channel logout | T8 | Out of scope for this phase |
| G12 | ~~The inherited Angular 21 workspace (`bff/ui/package-lock.json`) carried 102 Dependabot alerts: 2 critical (development-only), 38 high, including Angular 21.2.13 runtime advisories~~ | T14 | Closed 2026-10-10: the workspace was replaced by the Angular 22 Nx workspace with a fresh lockfile |
| G13 | URL query strings are exported to telemetry unredacted; on the Keycloak topology only development-server parameters appear | T15 | Rule for every endpoint: no secrets or personal data in URLs (ADR-0007). Enable query redaction before telemetry leaves the developer machine |
| G14 | The Keycloak hosting integration (`Aspire.Hosting.Keycloak` 13.6.1-preview) has no stable release, and Keycloak's own telemetry does not reach the dashboard yet | T14, T15 | Pinned version, suites B and C as the regression net; revisit on each Aspire update |
