import { bff } from './support/bff';
import { expect, signIn, test } from './support/fixtures';
import { identityProvider } from './support/identity-provider';
import { testUsers } from './support/test-users';

const user = testUsers.employee;

test('B05 staff user signs in through the identity provider', async ({ page }) => {
  await signIn(page, user);

  await expect(bff.userName(page)).toHaveText(identityProvider.displayNameOf(user));
  const profile = await page.request.get('/api/User');
  expect(await profile.json()).toMatchObject({ isAuthenticated: true });

  const cookies = await page.context().cookies(bff.origin);
  const byName = (name: string) => cookies.find((cookie) => cookie.name === name);
  expect(byName('.AspNetCore.Cookies'), 'session cookie').toMatchObject({
    httpOnly: true,
    secure: true,
    sameSite: 'Lax',
  });
  expect(byName('__Host-X-XSRF-TOKEN'), 'antiforgery cookie').toMatchObject({
    httpOnly: true,
    secure: true,
    sameSite: 'Strict',
  });
  expect(byName('XSRF-RequestToken'), 'readable request token').toMatchObject({
    httpOnly: false,
    secure: true,
    sameSite: 'Strict',
  });
});

test('B06 signed-in user calls a protected API through the UI', async ({ page }) => {
  await signIn(page, user);

  await bff.directApiButton(page).click();

  await expect(page.getByText('more data')).toBeVisible();
});

test('B07 sign-out ends both the BFF session and the identity provider session', async ({
  page,
}) => {
  await signIn(page, user);

  await bff.logoutButton(page).click();
  await identityProvider.completeSignOut(page);

  await expect(bff.loginLink(page)).toBeVisible();
  expect(await (await page.request.get('/api/User')).json()).toMatchObject({
    isAuthenticated: false,
  });
  expect((await page.request.get('/api/DirectApi')).status()).toBe(401);

  await bff.loginLink(page).click();
  await identityProvider.expectCredentialsPrompt(page);
});
