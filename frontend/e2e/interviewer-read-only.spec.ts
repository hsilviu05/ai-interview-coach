import { expect, test } from '@playwright/test';
import { login } from './support/auth';
import { replaceCatalogWithStarterSet } from './support/catalog';
import { TEST_USERS } from './support/test-env';

test('interviewer sees the problem catalog in read-only mode', async ({
  page,
  request,
}) => {
  await replaceCatalogWithStarterSet(request);
  await login(page, TEST_USERS.interviewer, '/interviewer');
  await page.goto('/interviewer/problems');

  await expect(
    page.getByRole('heading', { name: 'Browse reusable coding problems.' })
  ).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Problem management is admin-only.' })
  ).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Replace With Starter Catalog' })
  ).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Admin only' }).first()).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Two Sum' })).toBeVisible();
});
