import { expect, type APIRequestContext } from '@playwright/test';
import { E2E_BACKEND_URL, TEST_USERS } from './test-env';

export async function replaceCatalogWithStarterSet(
  request: APIRequestContext
): Promise<void> {
  await loginAsAdmin(request);
  const response = await request.post(
    `${E2E_BACKEND_URL}/api/problems/catalog/replace-with-starter-set`,
    { data: {} }
  );

  expect(response.ok()).toBeTruthy();
}

async function loginAsAdmin(request: APIRequestContext): Promise<void> {
  const response = await request.post(`${E2E_BACKEND_URL}/api/auth/login`, {
    data: {
      email: TEST_USERS.admin.email,
      password: TEST_USERS.admin.password,
    },
  });

  expect(response.ok()).toBeTruthy();
}
