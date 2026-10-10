#!/usr/bin/env bash
# Runs Casevanta's automated checks, fastest first.
#
#   scripts/test.sh            everything: suites A, C and B plus the frontend checks (needs Docker)
#   scripts/test.sh --fast     no containers and no browser: suite A, architecture rules, frontend checks
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== Backend: API and security tests (suite A) and architecture rules"
dotnet test --project backend/tests/Casevanta.Api.Tests/Casevanta.Api.Tests.csproj
dotnet test --project backend/tests/Casevanta.Architecture.Tests/Casevanta.Architecture.Tests.csproj

echo "== Formatting of the whole repository (Prettier)"
pnpm -C frontend format:check

echo "== Frontend: lint, unit tests and production build"
pnpm -C frontend nx run-many -t lint test build --outputStyle=static

if [ "${1:-}" != "--fast" ]; then
  echo "== Distributed topology through Aspire (suite C)"
  dotnet test --project backend/tests/Casevanta.AppHost.Tests/Casevanta.AppHost.Tests.csproj

  echo "== Browser journeys through the BFF (suite B)"
  pnpm -C frontend nx e2e staff-web-e2e --outputStyle=static
fi
