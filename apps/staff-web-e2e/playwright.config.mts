import { workspaceRoot } from '@nx/devkit';
import { nxE2EPreset } from '@nx/playwright/preset';
import { defineConfig, devices } from '@playwright/test';

/**
 * Suite B: browser journeys through the BFF. Playwright starts the whole local topology with the
 * Aspire AppHost (identity provider, PostgreSQL, Angular dev server and the BFF at
 * https://localhost:5001) or reuses a running `aspire run`. The BFF only serves its host page once
 * the dev server answers, so a 200 on / means the stack is ready. Kept free of relative imports so
 * Nx can load it with Node's native TypeScript stripping.
 */
const bffOrigin = 'https://localhost:5001';

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
  webServer: {
    name: 'aspire',
    command: 'dotnet run --project backend/src/Casevanta.AppHost/Casevanta.AppHost.csproj',
    cwd: workspaceRoot,
    url: `${bffOrigin}/`,
    ignoreHTTPSErrors: true,
    reuseExistingServer: !process.env['CI'],
    timeout: 600_000,
    gracefulShutdown: { signal: 'SIGTERM', timeout: 30_000 },
  },
});
