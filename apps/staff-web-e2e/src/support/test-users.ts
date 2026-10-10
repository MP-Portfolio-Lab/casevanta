/**
 * Synthetic, development-only accounts. They exist only in disposable local identity stores
 * (a temporary SQLite file today, the imported Keycloak development realm later) and are not
 * credentials for any real system.
 */
export interface TestUser {
  readonly email: string;
  readonly password: string;
}

export const testUsers = {
  employee: {
    email: 'e2e.employee@casevanta.test',
    password: 'Casevanta-e2e-Pass-1!',
  },
} as const satisfies Record<string, TestUser>;
