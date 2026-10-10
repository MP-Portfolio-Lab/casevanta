#!/usr/bin/env bash
# Starts the full local topology with Aspire: PostgreSQL, the identity provider, the Angular dev
# server and the BFF at https://localhost:5001. The dashboard URL is printed on start.
#
#   scripts/dev.sh
set -euo pipefail
cd "$(dirname "$0")/.."
exec aspire run --apphost backend/src/Casevanta.AppHost/Casevanta.AppHost.csproj "$@"
