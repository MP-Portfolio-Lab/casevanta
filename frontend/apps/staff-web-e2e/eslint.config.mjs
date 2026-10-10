import playwright from 'eslint-plugin-playwright';
import baseConfig from '../../eslint.config.mjs';

export default [
  playwright.configs['flat/recommended'],
  ...baseConfig,
  {
    files: ['**/*.ts', '**/*.js'],
    rules: {},
  },
  {
    // Identity provider adapters and fixtures are shared steps that tests call; their
    // assertions run inside the calling test.
    files: ['src/support/**/*.ts'],
    rules: {
      'playwright/no-standalone-expect': 'off',
    },
  },
];
