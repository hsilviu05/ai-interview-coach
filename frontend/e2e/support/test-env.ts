import os from 'node:os';
import path from 'node:path';

export const E2E_FRONTEND_URL = 'http://127.0.0.1:4201';
export const E2E_BACKEND_URL = 'http://127.0.0.1:5292';
export const E2E_JWT_KEY =
  'playwright-e2e-jwt-key-32-characters-minimum-2026';
export const E2E_SQLITE_PATH = path.join(
  os.tmpdir(),
  'ai-interview-coach-playwright.db'
);

export const TEST_USERS = {
  admin: {
    email: 'playwright-admin@test.com',
    password: 'Password123!',
    fullName: 'Playwright Admin',
  },
  interviewer: {
    email: 'recruiter@test.com',
    password: 'Password123!',
    fullName: 'Recruiter Demo',
  },
  candidate: {
    email: 'candidate@test.com',
    password: 'Password123!',
    fullName: 'Candidate Demo',
  },
} as const;
