# ADR-0010: Configured public origin; forwarded headers only behind a known proxy

- Status: Accepted
- Date: 2026-10-10

## Context

The BFF builds OIDC `redirect_uri` and `post_logout_redirect_uri` from the request by default.
Behind a reverse proxy, or in integration tests with randomized ports, the request's origin is not
the origin registered in Keycloak.

## Decision

- `App:PublicOrigin` (an origin, no path) pins both redirect URIs when it is set (test A22). The
  AppHost sets `https://localhost:5001`, so suite C can run the BFF on any port.
- The CSP `form-action` allows the identity provider's origin (scheme, host, port), derived from the
  configured authority.
- Forwarded headers are not processed today. When a reverse proxy is introduced, the BFF enables
  `UseForwardedHeaders` for `X-Forwarded-For` and `X-Forwarded-Proto` only from configured
  `KnownProxies` or `KnownNetworks`, and never trusts `X-Forwarded-Host` without that restriction.
  HTTPS redirection and HSTS then follow the forwarded scheme.

## Consequences

- Redirect URIs cannot be steered by a client-supplied `Host` header.
- Each deployment must set `App:PublicOrigin` to its public HTTPS origin.
