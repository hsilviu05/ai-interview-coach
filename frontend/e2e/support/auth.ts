import { expect, type Page } from '@playwright/test';

export async function login(
  page: Page,
  credentials: { email: string; password: string },
  expectedPathPrefix: '/candidate' | '/interviewer'
): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill(credentials.email);
  await page.getByLabel('Password').fill(credentials.password);
  await page.getByRole('button', { name: 'Login' }).click();

  await expect(page).toHaveURL(new RegExp(`${expectedPathPrefix}/`));
}
