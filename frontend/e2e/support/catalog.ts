import { expect, type APIRequestContext } from '@playwright/test';
import { E2E_BACKEND_URL, TEST_USERS } from './test-env';

let adminTokenPromise: Promise<string> | null = null;

export async function replaceCatalogWithStarterSet(
  request: APIRequestContext
): Promise<void> {
  const token = await getAdminToken(request);
  const response = await request.post(
    `${E2E_BACKEND_URL}/api/problems/catalog/replace-with-starter-set`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      data: {},
    }
  );

  expect(response.ok()).toBeTruthy();
}

async function getAdminToken(request: APIRequestContext): Promise<string> {
  if (!adminTokenPromise) {
    adminTokenPromise = (async () => {
      const response = await request.post(`${E2E_BACKEND_URL}/api/auth/login`, {
        data: {
          email: TEST_USERS.admin.email,
          password: TEST_USERS.admin.password,
        },
      });

      expect(response.ok()).toBeTruthy();
      const payload = (await response.json()) as { token: string };
      return payload.token;
    })();
  }

  return adminTokenPromise;
}
