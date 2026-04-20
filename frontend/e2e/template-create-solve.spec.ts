import { expect, test } from '@playwright/test';
import { login } from './support/auth';
import { replaceCatalogWithStarterSet } from './support/catalog';
import { getEditorValue, replaceEditorValue } from './support/editor';
import { TEST_USERS } from './support/test-env';

const pythonTwoSumSolution = `from typing import List


class Solution:
    def twoSum(self, nums: List[int], target: int) -> List[int]:
        seen = {}

        for index, value in enumerate(nums):
            complement = target - value
            if complement in seen:
                return [seen[complement], index]
            seen[value] = index

        return []
`;

test('admin can create a template-based problem and a candidate can solve it in practice mode', async ({
  page,
  request,
}) => {
  const customTitle = `Playwright Template Two Sum ${Date.now()}`;

  await replaceCatalogWithStarterSet(request);

  await login(page, TEST_USERS.admin, '/interviewer');
  await page.goto('/interviewer/create-problem');

  await expect(
    page.getByRole('heading', { name: 'Create a reusable coding problem.' })
  ).toBeVisible();

  await page.getByRole('button', { name: 'Load Two Sum' }).click();

  await expect(page.locator('#title')).toHaveValue('Two Sum');
  await expect(page.locator('#topic')).toHaveValue('Arrays');
  await expect(page.locator('#pythonMethodName')).toHaveValue('twoSum');
  await expect
    .poll(async () => page.locator('#previewPythonStarterCode').inputValue())
    .toContain('def twoSum');

  await page.locator('#title').fill(customTitle);
  await page.locator('#description').fill(
    'Return the pair of indices whose values add up to the target.'
  );

  await page.getByRole('button', { name: 'Create Problem' }).click();

  await expect(page.getByRole('heading', { name: 'Problem Created' })).toBeVisible();
  await expect(page.getByText(customTitle)).toBeVisible();

  await page.locator('#input').fill('{"nums":[2,7,11,15],"target":9}');
  await page.locator('#expectedOutput').fill('[0, 1]');
  await page.getByRole('button', { name: 'Add Test Case' }).click();

  await expect(page.getByText('Test case added successfully.')).toBeVisible();

  await login(page, TEST_USERS.candidate, '/candidate');
  await page.goto('/candidate/practice');

  await page.getByLabel('Search').fill(customTitle);
  await expect(page.getByText('Showing 1 of 5 practice problems')).toBeVisible();
  await expect(page.getByRole('heading', { name: customTitle })).toBeVisible();

  await page.getByRole('button', { name: 'Practice Problem' }).click();

  await expect(page).toHaveURL(/\/candidate\/practice\//);
  await expect(page.getByRole('heading', { name: customTitle })).toBeVisible();

  await page.getByLabel('Language').selectOption('python');
  await expect
    .poll(async () => getEditorValue(page))
    .toContain('def twoSum');

  await replaceEditorValue(page, pythonTwoSumSolution);
  await page.getByRole('button', { name: 'Save Practice Submission' }).click();

  await expect(page.getByText('Problem accepted. Practice complete.')).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Practice problem completed.' })
  ).toBeVisible();
});
