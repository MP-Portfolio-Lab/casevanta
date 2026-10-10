#!/usr/bin/env bash
# Runs Casevanta's automated checks, fastest first.
#
#   scripts/test.sh            everything, including the browser journeys (suite B)
#   scripts/test.sh --fast     skips the browser journeys
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== Backend: build and API/security tests (suite A)"
dotnet test --solution backend/Casevanta.slnx

echo "== Frontend: formatting, lint, unit tests and production build"
pnpm nx format:check --all
pnpm nx run-many -t lint test build

if [ "${1:-}" != "--fast" ]; then
  echo "== Browser journeys through the BFF (suite B)"
  pnpm nx e2e staff-web-e2e
fi
