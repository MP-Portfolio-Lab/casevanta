#!/usr/bin/env bash
# One-command developer setup for Casevanta. Checks prerequisites, installs dependencies and git
# hooks, and trusts the ASP.NET Core development certificate. Safe to re-run.
#
#   scripts/bootstrap.sh
set -euo pipefail
cd "$(dirname "$0")/.."

ok() { printf '  \033[32m✓\033[0m %s\n' "$*"; }
fail() { printf '  \033[31m✗\033[0m %s\n' "$*" >&2; exit 1; }

echo "Checking prerequisites"
command -v dotnet >/dev/null || fail "dotnet is missing: install the .NET SDK pinned in global.json"
dotnet --version >/dev/null 2>&1 || fail "The .NET SDK pinned in global.json is not installed (dotnet --list-sdks)"
ok ".NET SDK $(dotnet --version)"

command -v node >/dev/null || fail "Node.js is missing: install the version in .nvmrc (Node 22.22.3+, 24.15+ or 26+)"
node -e '
  const [major, minor, patch] = process.versions.node.split(".").map(Number);
  const supported = (major === 22 && (minor > 22 || (minor === 22 && patch >= 3)))
    || (major === 24 && minor >= 15) || major >= 26;
  process.exit(supported ? 0 : 1);
' || fail "Node.js $(node --version) is not supported by Angular 22: use 22.22.3+, 24.15+ or 26+"
ok "Node.js $(node --version)"

command -v pnpm >/dev/null || fail "pnpm is missing: install pnpm 12 (brew install pnpm, or corepack enable)"
[ "$(pnpm --version | cut -d. -f1)" -ge 12 ] || fail "pnpm $(pnpm --version) is too old: pnpm 12 is required"
ok "pnpm $(pnpm --version)"

command -v gitleaks >/dev/null || fail "gitleaks is missing: the pre-commit secret scan needs it (brew install gitleaks)"
ok "gitleaks $(gitleaks version)"

command -v aspire >/dev/null || fail "The Aspire CLI is missing: curl -sSL https://aspire.dev/install.sh | bash (or: dotnet tool install -g Aspire.Cli)"
aspire_version=$(aspire --version | cut -d+ -f1)
node -e 'const [a,b,c]=process.argv[1].split(".").map(Number); process.exit(a>13||(a===13&&(b>6||(b===6&&c>=1)))?0:1)' "$aspire_version" \
  || fail "Aspire CLI $aspire_version is too old: 13.6.1 or later is required (aspire update --self)"
ok "Aspire CLI $aspire_version"

if docker info >/dev/null 2>&1; then
  ok "Docker $(docker info --format '{{.ServerVersion}}')"
else
  echo "  ! Docker is not running: the Aspire orchestration and its tests need it"
fi

echo "Installing dependencies"
pnpm install --frozen-lockfile
dotnet restore backend/Casevanta.slnx
pnpm exec playwright install chromium

echo "Configuring the development environment"
if dotnet dev-certs https --check --trust >/dev/null 2>&1; then
  ok "ASP.NET Core development certificate is trusted"
else
  dotnet dev-certs https --trust
fi
pnpm exec lefthook install
ok "git hooks installed (gitleaks and Prettier on pre-commit)"

echo
echo "Done. Next: scripts/test.sh runs every automated check; see README.md to start the app."
