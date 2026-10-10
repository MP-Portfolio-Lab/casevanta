# ADR-0001: Start from a sanitised import of damienbod's BFF sample

- Status: Accepted
- Date: 2026-10-10

## Context

Casevanta needs a secure browser-facing backend (BFF) for its Angular applications: a cookie
session, server-side tokens, antiforgery and a strict Content Security Policy. damienbod's
`bff-openiddict-aspnetcore-angular` (Apache-2.0) implements exactly that and is maintained, but its
history contains a committed TLS private key, an Nx Cloud access token and a user database, and it
carries names, demo code and an identity server that Casevanta does not want.

## Decision

- Import upstream commit `08a8d384` as a single commit with fresh history, excluding the private
  key, the SQLite user database and the stale build output. `gitleaks` must report the public
  history clean before the first push.
- Keep the full upstream history only in local backups (a branch, a tag and a git bundle), never
  pushed.
- Keep the Apache-2.0 licence; `NOTICE` names the upstream project, the imported commit and the
  excluded files.

## Consequences

- The public history starts clean and stays clean (CI and a pre-commit hook run gitleaks).
- Upstream fixes are not merged automatically; they are reviewed and ported by hand.
- Attribution obligations are met through `LICENSE`, `NOTICE` and the README.

## Alternatives considered

- **Keep the 132 upstream commits.** Rejected: rewriting them to remove the secrets would still
  publish a long history that is unrelated to Casevanta.
- **Start from an empty repository.** Rejected: the BFF security mechanics are subtle and already
  proven upstream; the characterisation tests (ADR-0004) pin them down instead.
