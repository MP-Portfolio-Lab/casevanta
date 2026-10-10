#!/usr/bin/env bash
# Applies Casevanta's GitHub repository protection. Idempotent: safe to re-run after changes.
#
#   scripts/github-protect.sh [owner/repo]
#
# REQUIRED_CHECKS overrides the required status checks (space-separated CI job names).
# The owner can still push directly (enforce_admins is off); pull requests must pass the checks.
set -euo pipefail

repo="${1:-MattWUchanski/casevanta}"
branch="main"
read -r -a checks <<< "${REQUIRED_CHECKS:-secrets backend web integration e2e}"

echo "Repository: $repo"

echo "- Dependabot alerts and security updates"
gh api --method PUT "repos/$repo/vulnerability-alerts" --silent
gh api --method PUT "repos/$repo/automated-security-fixes" --silent

echo "- Secret scanning and push protection"
gh api --method PATCH "repos/$repo" --silent --input - <<'JSON'
{"security_and_analysis": {"secret_scanning": {"status": "enabled"}, "secret_scanning_push_protection": {"status": "enabled"}}}
JSON

echo "- CodeQL default setup"
gh api --method PATCH "repos/$repo/code-scanning/default-setup" --silent -f state=configured -f query_suite=default

echo "- Branch protection on $branch, required checks: ${checks[*]}"
contexts=$(printf '"%s",' "${checks[@]}")
gh api --method PUT "repos/$repo/branches/$branch/protection" --silent --input - <<JSON
{
  "required_status_checks": {"strict": false, "contexts": [${contexts%,}]},
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": true
}
JSON

echo
echo "Current state:"
gh api "repos/$repo" --jq '.security_and_analysis | {secret_scanning: .secret_scanning.status, push_protection: .secret_scanning_push_protection.status, dependabot_security_updates: .dependabot_security_updates.status}'
gh api "repos/$repo/branches/$branch/protection" --jq '{required_checks: .required_status_checks.contexts, linear_history: .required_linear_history.enabled, force_pushes: .allow_force_pushes.enabled, deletions: .allow_deletions.enabled, enforce_admins: .enforce_admins.enabled}'
gh api "repos/$repo/code-scanning/default-setup" --jq '{codeql: .state, languages, query_suite}'
