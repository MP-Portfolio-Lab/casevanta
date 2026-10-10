# Keycloak (development)

The Aspire AppHost runs Keycloak (`quay.io/keycloak/keycloak`, `start-dev`) on
`http://localhost:8080` and imports every realm file in `realms/` on start. There is no data
volume yet: each start re-imports the realm, so edits to the JSON take effect on the next run.

## Realm `casevanta-workforce`

| Item | Value |
|---|---|
| Client | `staff-web`: confidential, authorization code + PKCE (S256), redirect `https://localhost:5001/signin-oidc`, post-logout redirect `https://localhost:5001/signout-callback-oidc` |
| Client secret | `${STAFF_WEB_CLIENT_SECRET}`, generated per machine by the AppHost parameter `staff-web-client-secret` (kept in the AppHost's user secrets) and given to both Keycloak and the BFF |
| Roles | `employee`, `verifier`, `inspector`, `auditor`, `administrator`, emitted as a flat `roles` claim in ID and access tokens |
| Sessions | SSO idle 30 minutes, maximum 10 hours; access tokens 5 minutes |

## Development users

**These accounts and the shared password are for local development only.** They exist only in the
disposable container started by the AppHost and must never be used elsewhere.

| Username | Name | Roles |
|---|---|---|
| `ada.employee` | Ada Employee | employee |
| `victor.verifier` | Victor Verifier | employee, verifier |
| `iris.inspector` | Iris Inspector | employee, inspector |
| `aaron.auditor` | Aaron Auditor | auditor |
| `alice.admin` | Alice Administrator | administrator |

Password for every account: `Casevanta-dev-1!`

The Keycloak admin console (`http://localhost:8080`) uses the user `admin` and the generated AppHost
parameter `keycloak-password` (`aspire secret list`, or the dashboard's parameters view).

## Changing the realm

Edit the JSON, or change the realm in the admin console and export it:

```bash
docker exec <keycloak-container> /opt/keycloak/bin/kc.sh export --dir /tmp/export --realm casevanta-workforce --users realm_file
docker cp <keycloak-container>:/tmp/export/casevanta-workforce-realm.json infra/keycloak/realms/
```

Then restore the `${STAFF_WEB_CLIENT_SECRET}` placeholder: an export contains the generated secret.
