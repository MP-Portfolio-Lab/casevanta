import { expect, request, type Page } from '@playwright/test';
import { bff } from './bff';
import type { TestUser } from './test-users';

/** What the journeys need from an identity provider; one adapter per provider. */
export interface IdentityProviderAdapter {
  readonly name: string;
  readonly origin: string;
  /** Waits until the provider serves its discovery document. */
  waitUntilReady(timeoutMs?: number): Promise<void>;
  /** The display name the BFF reports for the user. */
  displayNameOf(user: TestUser): string;
  /** Completes sign-in on the provider's pages and returns to the BFF. */
  signIn(page: Page, user: TestUser): Promise<void>;
  /** Completes sign-out on the provider's pages and returns to the BFF. */
  completeSignOut(page: Page): Promise<void>;
  /** Asserts the provider asks for credentials again, i.e. its session really ended. */
  expectCredentialsPrompt(page: Page): Promise<void>;
}

const realm = 'casevanta-workforce';

const keycloak: IdentityProviderAdapter = {
  name: 'keycloak',
  origin: 'https://localhost:8080',

  async waitUntilReady(timeoutMs = 180_000) {
    await waitForDiscoveryDocument(
      `${this.origin}/realms/${realm}/.well-known/openid-configuration`,
      timeoutMs,
    );
  },

  displayNameOf: (user) => user.displayName,

  async signIn(page, user) {
    await page.waitForURL(
      (url) => url.origin === this.origin && url.pathname.startsWith(`/realms/${realm}/`),
    );
    await page.locator('#username').fill(user.username);
    await page.locator('#password').fill(user.password);
    await page.locator('#kc-login').click();
    await page.waitForURL(`${bff.origin}/`);
  },

  async completeSignOut(page) {
    // The BFF sends id_token_hint and a registered post-logout URI, so Keycloak signs out and
    // redirects straight back without a confirmation page.
    await page.waitForURL(`${bff.origin}/`);
  },

  async expectCredentialsPrompt(page) {
    await page.waitForURL(
      (url) => url.origin === this.origin && url.pathname.startsWith(`/realms/${realm}/`),
    );
    await expect(page.locator('#password')).toBeVisible();
  },
};

async function waitForDiscoveryDocument(url: string, timeoutMs: number): Promise<void> {
  const client = await request.newContext({ ignoreHTTPSErrors: true });
  try {
    await expect
      .poll(
        async () => {
          try {
            return (await client.get(url, { timeout: 5_000 })).status();
          } catch {
            return 0;
          }
        },
        { timeout: timeoutMs, message: `identity provider discovery document at ${url}` },
      )
      .toBe(200);
  } finally {
    await client.dispose();
  }
}

const adapters: Record<string, IdentityProviderAdapter> = { keycloak };

const selected = process.env['E2E_IDP'] ?? 'keycloak';
const adapter = adapters[selected];
if (!adapter) {
  throw new Error(`Unknown E2E_IDP '${selected}'. Known: ${Object.keys(adapters).join(', ')}`);
}

export const identityProvider: IdentityProviderAdapter = adapter;
