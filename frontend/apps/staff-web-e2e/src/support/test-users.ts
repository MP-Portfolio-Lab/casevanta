/**
 * Synthetic, development-only accounts from the Keycloak realm import
 * (infra/keycloak/realms/casevanta-workforce-realm.json). They exist only in the disposable
 * container that the AppHost starts and are not credentials for any real system.
 */
export interface TestUser {
  readonly username: string;
  readonly password: string;
  readonly displayName: string;
  readonly roles: readonly string[];
}

const developmentPassword = 'Casevanta-dev-1!';

export const testUsers = {
  employee: {
    username: 'ada.employee',
    password: developmentPassword,
    displayName: 'Ada Employee',
    roles: ['employee'],
  },
  verifier: {
    username: 'victor.verifier',
    password: developmentPassword,
    displayName: 'Victor Verifier',
    roles: ['employee', 'verifier'],
  },
} as const satisfies Record<string, TestUser>;
