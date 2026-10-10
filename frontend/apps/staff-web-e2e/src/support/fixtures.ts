import { test as base, expect, type Page } from '@playwright/test';
import { bff } from './bff';
import { identityProvider } from './identity-provider';
import type { TestUser } from './test-users';

/**
 * B08 guard, applied to every test: a Content Security Policy violation or an uncaught page
 * error fails the test, so UI or header changes cannot silently weaken the policy.
 */
export const test = base.extend<{ securityGuard: void }>({
  securityGuard: [
    async ({ page }, use) => {
      const problems: string[] = [];
      page.on('console', (message) => {
        if (/Content Security Policy|Refused to/i.test(message.text())) {
          problems.push(`[console.${message.type()}] ${message.text()}`);
        }
      });
      page.on('pageerror', (error) => problems.push(`[pageerror] ${error.message}`));
      await use();
      expect(problems, 'CSP violations or uncaught page errors').toEqual([]);
    },
    { auto: true },
  ],
});

export { expect };

/** Signs in through the BFF login link and the identity provider, ending on the app shell. */
export async function signIn(page: Page, user: TestUser): Promise<void> {
  await page.goto('/');
  await bff.loginLink(page).click();
  await identityProvider.signIn(page, user);
  await expect(bff.logoutButton(page)).toBeVisible();
}
