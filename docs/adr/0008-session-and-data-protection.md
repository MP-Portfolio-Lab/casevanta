# ADR-0008: Server-side cookie session and Data Protection keys

- Status: Accepted
- Date: 2026-10-10

## Context

The BFF keeps the session in an encrypted, HttpOnly cookie and stores the ID, access and refresh
tokens inside it (`SaveTokens`). Cookies are encrypted with ASP.NET Core Data Protection, whose keys
live in the user profile by default and would be lost when a container restarts.

## Decision

- Session cookie: Secure always, HttpOnly, SameSite=Lax, 8-hour sliding lifetime (test A25).
  Keycloak's SSO session: 30 minutes idle, 10 hours maximum. Access tokens: 5 minutes.
- Data Protection application name `casevanta`, so every instance of the BFF can read the same
  cookies.
- Key storage per environment: development keeps the default per-user location; any deployment
  persists keys in the shared PostgreSQL database (or blob storage) and protects them at rest with a
  key-management service, with the default 90-day rotation.
- Tokens are not refreshed while the BFF calls no downstream API with them. The first downstream call
  introduces refresh handling and, with it, a server-side ticket store, which also makes sessions
  revocable and keeps cookies small.

## Consequences

- Restarts and scale-out do not log users out once deployment key storage exists.
- Until the ticket store exists, a copied cookie stays valid until it expires, even after logout
  (threat model G3).
