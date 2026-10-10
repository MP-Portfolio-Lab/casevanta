import { mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, devices } from '@playwright/test';
import { bff } from './support/bff';
import { identityProvider } from './support/identity-provider';

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, '..');
const tmpDir = path.join(here, '.tmp');
mkdirSync(tmpDir, { recursive: true });

const uiDevServer = 'http://localhost:4201';
const reuseExistingServer = !process.env.CI;

/**
 * Suite B harness: the identity provider, the Angular dev server (plain HTTP on 4201) and the BFF,
 * whose Development settings proxy the dev server. The journeys themselves never change between
 * migration steps; only this wiring does.
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  outputDir: 'test-results',
  globalSetup: './global-setup.ts',
  use: {
    baseURL: bff.origin,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], ignoreHTTPSErrors: true } }],
  webServer: [
    {
      name: 'identity-provider',
      command: 'dotnet run',
      cwd: path.join(repoRoot, 'IdentityProvider'),
      env: {
        ConnectionStrings__DefaultConnection: `Data Source=${path.join(tmpDir, 'identity-provider.sqlite')}`,
      },
      url: `${identityProvider.origin}/.well-known/openid-configuration`,
      ignoreHTTPSErrors: true,
      reuseExistingServer,
      timeout: 240_000,
    },
    {
      name: 'staff-ui',
      command: 'npx ng serve --port 4201 --host localhost',
      cwd: path.join(repoRoot, 'bff', 'ui'),
      url: `${uiDevServer}/`,
      reuseExistingServer,
      timeout: 240_000,
    },
    {
      name: 'bff',
      command: 'dotnet run',
      cwd: path.join(repoRoot, 'backend', 'src', 'Casevanta.Api'),
      url: `${bff.origin}/`,
      ignoreHTTPSErrors: true,
      reuseExistingServer,
      timeout: 240_000,
    },
  ],
});
