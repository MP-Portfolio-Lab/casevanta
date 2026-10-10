import { chromium } from '@playwright/test';
import { identityProvider } from './support/identity-provider';
import { testUsers } from './support/test-users';

/** Makes sure every synthetic test user exists in the identity provider under test. */
export default async function globalSetup(): Promise<void> {
  const browser = await chromium.launch();
  try {
    for (const user of Object.values(testUsers)) {
      const context = await browser.newContext({ ignoreHTTPSErrors: true });
      await identityProvider.ensureUser(await context.newPage(), user);
      await context.close();
    }
  } finally {
    await browser.close();
  }
}
