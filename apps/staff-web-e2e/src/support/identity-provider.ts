import { expect, type Page } from '@playwright/test';
import { bff } from './bff';
import type { TestUser } from './test-users';

/** What the journeys need from an identity provider; one adapter per provider. */
export interface IdentityProviderAdapter {
  readonly name: string;
  readonly origin: string;
  /** Makes sure the user can sign in (registers it if the provider allows self-registration). */
  ensureUser(page: Page, user: TestUser): Promise<void>;
  /** The display name the BFF reports for the user. */
  displayNameOf(user: TestUser): string;
  /** Completes sign-in on the provider's pages, including consent, and returns to the BFF. */
  signIn(page: Page, user: TestUser): Promise<void>;
  /** Completes sign-out on the provider's pages and returns to the BFF. */
  completeSignOut(page: Page): Promise<void>;
  /** Asserts the provider asks for credentials again, i.e. its session really ended. */
  expectCredentialsPrompt(page: Page): Promise<void>;
}

const openIddict: IdentityProviderAdapter = {
  name: 'openiddict',
  origin: 'https://localhost:44318',

  async ensureUser(page, user) {
    await page.goto(`${this.origin}/Identity/Account/Register`);
    await page.locator('#Input_Email').fill(user.email);
    await page.locator('#Input_Password').fill(user.password);
    await page.locator('#Input_ConfirmPassword').fill(user.password);
    await page.locator('#registerSubmit').click();
    await page.waitForLoadState();
    if (new URL(page.url()).pathname.endsWith('/Register')) {
      // Registration is idempotent across runs: an existing account is fine, anything else is not.
      await expect(page.locator('.validation-summary-errors, .text-danger').first()).toContainText(
        /already taken/i,
      );
    }
  },

  displayNameOf: (user) => user.email,

  async signIn(page, user) {
    await page.waitForURL(
      (url) => url.origin === this.origin && url.pathname.startsWith('/Identity/Account/Login'),
    );
    await page.locator('#Input_Email').fill(user.email);
    await page.locator('#Input_Password').fill(user.password);
    await page.locator('#login-submit').click();

    // The first sign-in asks for consent; later ones go straight back to the BFF.
    const consent = page.locator('input[name="submit.Accept"]');
    await expect(async () => {
      const backAtBff = new URL(page.url()).origin === bff.origin;
      expect(backAtBff || (await consent.isVisible())).toBe(true);
    }).toPass({ timeout: 30_000 });
    if (await consent.isVisible()) {
      await consent.click();
    }
    await page.waitForURL(`${bff.origin}/`);
  },

  async completeSignOut(page) {
    await page.waitForURL(
      (url) => url.origin === this.origin && url.pathname === '/connect/logout',
    );
    await page.locator('input[name="Confirm"]').click();
    await page.waitForURL(`${bff.origin}/`);
  },

  async expectCredentialsPrompt(page) {
    await page.waitForURL(
      (url) => url.origin === this.origin && url.pathname.startsWith('/Identity/Account/Login'),
    );
    await expect(page.locator('#Input_Password')).toBeVisible();
  },
};

const adapters: Record<string, IdentityProviderAdapter> = { openiddict: openIddict };

const selected = process.env.E2E_IDP ?? 'openiddict';
const adapter = adapters[selected];
if (!adapter) {
  throw new Error(`Unknown E2E_IDP '${selected}'. Known: ${Object.keys(adapters).join(', ')}`);
}

export const identityProvider: IdentityProviderAdapter = adapter;
