#!/usr/bin/env bash
# Returns the local environment to a clean state. Development data is disposable: PostgreSQL and
# Keycloak run without volumes, so the next start creates an empty database and re-imports the
# realm (infra/keycloak/realms), which is the single source of development users.
#
#   scripts/reset-dev-data.sh             stop the AppHost and remove local test output and caches
#   scripts/reset-dev-data.sh --secrets   also clear the AppHost's generated parameters (client
#                                         secret, Keycloak admin and database passwords), so the next
#                                         start generates new ones
set -euo pipefail
cd "$(dirname "$0")/.."
apphost=backend/src/Casevanta.AppHost/Casevanta.AppHost.csproj

echo "Stopping the AppHost, if it is running"
aspire stop --apphost "$apphost" --non-interactive --nologo >/dev/null 2>&1 || true

echo "Removing local test output and caches"
rm -rf tmp dist/.playwright coverage
pnpm nx reset >/dev/null

if [ "${1:-}" = "--secrets" ]; then
  echo "Clearing the AppHost's generated parameters"
  dotnet user-secrets clear --project "$apphost"
fi

echo "Done. The next scripts/dev.sh starts with an empty database and a freshly imported realm."
