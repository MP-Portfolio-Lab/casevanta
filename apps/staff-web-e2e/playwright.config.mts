import { mkdirSync } from 'node:fs';
import path from 'node:path';
import { workspaceRoot } from '@nx/devkit';
import { nxE2EPreset } from '@nx/playwright/preset';
import { defineConfig, devices } from '@playwright/test';

/**
 * Suite B: browser journeys through the BFF. Playwright starts the identity provider, the
 * Angular dev server (plain HTTP on 4201) and the BFF (https://localhost:5001), whose
 * Development settings proxy the dev server. The journeys stay the same between migration
 * steps; only this wiring changes. Kept free of relative imports so Nx can load it with
 * Node's native TypeScript stripping.
 */
const bffOrigin = 'https://localhost:5001';
const identityProviderOrigin = 'https://localhost:44318';
const devServer = 'http://localhost:4201';
const tmpDir = path.join(workspaceRoot, 'tmp', 'e2e');
mkdirSync(tmpDir, { recursive: true });
const reuseExistingServer = !process.env['CI'];

export default defineConfig({
  ...nxE2EPreset(import.meta.dirname, { testDir: './src' }),
  globalSetup: './src/global-setup.ts',
  fullyParallel: false,
  workers: 1,
  retries: process.env['CI'] ? 1 : 0,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: bffOrigin,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], ignoreHTTPSErrors: true } }],
  webServer: [
    {
      name: 'identity-provider',
      command: 'dotnet run',
      cwd: path.join(workspaceRoot, 'IdentityProvider'),
      env: {
        ConnectionStrings__DefaultConnection: `Data Source=${path.join(tmpDir, 'identity-provider.sqlite')}`,
      },
      url: `${identityProviderOrigin}/.well-known/openid-configuration`,
      ignoreHTTPSErrors: true,
      reuseExistingServer,
      timeout: 240_000,
    },
    {
      name: 'staff-web',
      command: 'pnpm exec nx run staff-web:serve',
      cwd: workspaceRoot,
      url: `${devServer}/`,
      reuseExistingServer,
      timeout: 240_000,
    },
    {
      name: 'bff',
      command: 'dotnet run',
      cwd: path.join(workspaceRoot, 'backend', 'src', 'Casevanta.Api'),
      url: `${bffOrigin}/`,
      ignoreHTTPSErrors: true,
      reuseExistingServer,
      timeout: 240_000,
    },
  ],
});
