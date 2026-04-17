import { expect, test } from '@playwright/test';
import { login } from './support/auth';
import { replaceCatalogWithStarterSet } from './support/catalog';
import { getEditorValue, replaceEditorValue } from './support/editor';
import { TEST_USERS } from './support/test-env';

const customPythonDraft = `from typing import List


class Solution:
    def maxProfit(self, prices: List[int]) -> int:
        best_profit = 0
        lowest_price = float("inf")

        for price in prices:
            lowest_price = min(lowest_price, price)
            best_profit = max(best_profit, price - lowest_price)

        return best_profit
`;

test('candidate can search practice problems, switch languages, and reset a draft', async ({
  page,
  request,
}) => {
  await replaceCatalogWithStarterSet(request);
  await login(page, TEST_USERS.candidate, '/candidate');
  await page.goto('/candidate/practice');

  await expect(
    page.getByRole('heading', {
      name: 'Browse the available coding challenges and practice anytime.',
    })
  ).toBeVisible();

  await page.getByLabel('Search').fill('stock');
  await expect(page.getByText('Showing 1 of 4 practice problems')).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Best Time to Buy and Sell Stock' })
  ).toBeVisible();

  await page.getByRole('button', { name: 'Practice Problem' }).click();

  await expect(page).toHaveURL(/\/candidate\/practice\//);
  await expect(
    page.getByRole('heading', { name: 'Best Time to Buy and Sell Stock' })
  ).toBeVisible();
  await expect(page.getByText('Example Output')).toBeVisible();

  await expect
    .poll(async () => getEditorValue(page))
    .toContain('public int MaxProfit');

  await page.getByLabel('Language').selectOption('python');
  await expect(
    page.getByText(
      'Implement the requested Python method only. A hidden runner handles input parsing and output formatting.'
    )
  ).toBeVisible();
  await expect.poll(async () => getEditorValue(page)).toContain('def maxProfit');

  await replaceEditorValue(page, customPythonDraft);
  await expect.poll(async () => getEditorValue(page)).toContain('best_profit = 0');
  await expect(page.getByText(/^Autosaved at /)).toBeVisible();

  await page.getByLabel('Language').selectOption('csharp');
  await expect.poll(async () => getEditorValue(page)).toContain('public int MaxProfit');

  await page.getByLabel('Language').selectOption('python');
  await expect.poll(async () => getEditorValue(page)).toContain('best_profit = 0');

  let dialogMessage = '';
  page.once('dialog', async dialog => {
    dialogMessage = dialog.message();
    await dialog.accept();
  });

  await page.getByRole('button', { name: 'Reset Problem' }).click();

  await expect.poll(() => dialogMessage).toContain(
    'Reset Best Time to Buy and Sell Stock? This removes your saved submissions and local draft for this practice problem.'
  );

  await expect(
    page.getByText('Best Time to Buy and Sell Stock was reset. You can start fresh now.')
  ).toBeVisible();
  await expect.poll(async () => getEditorValue(page)).toContain('public int MaxProfit');

  await page.getByLabel('Language').selectOption('python');
  await expect.poll(async () => getEditorValue(page)).toContain('def maxProfit');

  const resetValue = await getEditorValue(page);
  expect(resetValue).not.toContain('best_profit = 0');
});
