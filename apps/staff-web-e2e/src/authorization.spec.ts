import { expect, signIn, test } from './support/fixtures';
import { testUsers } from './support/test-users';

test('B09 a verifier may use the verification endpoint', async ({ page }) => {
  await signIn(page, testUsers.verifier);

  const ping = await page.request.get('/api/verification/ping');

  expect(ping.status()).toBe(200);
  const profile = await (await page.request.get('/api/User')).json();
  expect(
    profile.claims
      .filter((claim: { type: string }) => claim.type === profile.roleClaimType)
      .map((claim: { value: string }) => claim.value)
      .sort(),
  ).toEqual([...testUsers.verifier.roles].sort());
});

test('B10 other staff are refused by the verification endpoint', async ({ page }) => {
  await signIn(page, testUsers.employee);

  const ping = await page.request.get('/api/verification/ping');

  expect(ping.status()).toBe(403);
});
