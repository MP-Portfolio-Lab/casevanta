import { bff } from './support/bff';
import { expect, test } from './support/fixtures';

test('B01 anonymous visitor gets the application shell with a login link', async ({ page }) => {
  const response = await page.goto('/');

  expect(response?.status()).toBe(200);
  await expect(bff.loginLink(page)).toBeVisible();
  await expect(bff.logoutButton(page)).toHaveCount(0);
});

test('B02 anonymous API access is reported and rejected', async ({ request }) => {
  const user = await request.get('/api/User');
  expect(user.status()).toBe(200);
  expect(await user.json()).toMatchObject({ isAuthenticated: false });

  const data = await request.get('/api/DirectApi');
  expect(data.status()).toBe(401);
});

test('B03 client-side deep links fall back to the host page', async ({ request }) => {
  const response = await request.get('/cases/42/review');

  expect(response.status()).toBe(200);
  const html = await response.text();
  expect(html).toMatch(/ngCspNonce="[^"*]+"/i); // the Angular dev server lowercases attribute names
  expect(html).not.toContain('PLACEHOLDER_NONCE_SERVER');
});

test('B04 responses do not advertise the server implementation', async ({ request }) => {
  const response = await request.get('/');

  expect(response.headers()['server']).toBeUndefined();
});
