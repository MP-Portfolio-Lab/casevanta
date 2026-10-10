import type { Page } from '@playwright/test';

/**
 * The browser-facing contract of the BFF. Every UI generation keeps these elements, which lets
 * the same journeys run against the original Angular app and its replacement.
 */
export const bff = {
  origin: 'https://localhost:5001',
  loginLink: (page: Page) => page.locator('a[href="api/Account/Login"]'),
  logoutButton: (page: Page) =>
    page.locator('form[action="api/Account/Logout"] button[type="submit"]'),
  directApiButton: (page: Page) => page.getByRole('button', { name: /direct data/i }),
  userName: (page: Page) => page.getByTestId('user-name'),
};
