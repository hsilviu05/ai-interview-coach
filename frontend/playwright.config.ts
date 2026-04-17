import { defineConfig } from '@playwright/test';
import path from 'node:path';
import {
  E2E_BACKEND_URL,
  E2E_FRONTEND_URL,
  E2E_JWT_KEY,
  E2E_SQLITE_PATH,
  TEST_USERS,
} from './e2e/support/test-env';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  outputDir: './test-results',
  reporter: process.env.CI
    ? [['list'], ['html', { open: 'never' }]]
    : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: E2E_FRONTEND_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  webServer: [
    {
      command:
        'dotnet run --no-launch-profile --no-restore --urls http://127.0.0.1:5292',
      cwd: path.resolve(__dirname, '../backend/AIInterviewCoach.API'),
      url: `${E2E_BACKEND_URL}/health`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'IntegrationTesting',
        BootstrapAdmin__Email: TEST_USERS.admin.email,
        BootstrapAdmin__FullName: TEST_USERS.admin.fullName,
        BootstrapAdmin__Password: TEST_USERS.admin.password,
        ConnectionStrings__DefaultConnection: `Data Source=${E2E_SQLITE_PATH}`,
        DatabaseProvider: 'sqlite',
        Jwt__Key: E2E_JWT_KEY,
        ResetDatabaseOnStartup: 'true',
      },
    },
    {
      command: 'npm run start:e2e',
      cwd: __dirname,
      url: `${E2E_FRONTEND_URL}/login`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: {
        ...process.env,
        CI: 'true',
      },
    },
  ],
});
