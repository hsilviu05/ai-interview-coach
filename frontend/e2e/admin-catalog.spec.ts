import { expect, test } from '@playwright/test';
import { login } from './support/auth';
import { TEST_USERS } from './support/test-env';

test('admin can replace the starter catalog and see the audit entry', async ({
  page,
}) => {
  await login(page, TEST_USERS.admin, '/interviewer');
  await page.goto('/interviewer/problems');

  await expect(
    page.getByRole('heading', { name: 'Manage reusable coding problems.' })
  ).toBeVisible();

  let dialogMessage = '';
  page.once('dialog', async dialog => {
    dialogMessage = dialog.message();
    await dialog.accept();
  });

  await page
    .getByRole('button', { name: 'Replace With Starter Catalog' })
    .click();

  await expect.poll(() => dialogMessage).toContain(
    'Replace the entire catalog with a fresh starter set?'
  );

  await expect(page.getByText(/^Catalog replaced\./)).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Two Sum' })).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Best Time to Buy and Sell Stock' })
  ).toBeVisible();
  await expect(page.locator('.audit-list')).toContainText(
    'Replaced the problem catalog with the starter set.'
  );
});
