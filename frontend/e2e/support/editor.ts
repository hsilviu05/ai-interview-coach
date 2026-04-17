import { expect, type Page } from '@playwright/test';

export async function getEditorValue(page: Page): Promise<string> {
  return page.getByTestId('code-editor-value').evaluate(element =>
    (element as HTMLTextAreaElement).value
  );
}

export async function replaceEditorValue(
  page: Page,
  nextValue: string
): Promise<void> {
  const fallbackEditor = page.getByTestId('code-editor-textarea');

  if (await fallbackEditor.count()) {
    await fallbackEditor.fill(nextValue);
    return;
  }

  const monacoEditor = page.locator('.monaco-editor').first();
  await expect(monacoEditor).toBeVisible();
  await monacoEditor.click();
  await page.keyboard.press('ControlOrMeta+A');
  await page.keyboard.type(nextValue);
}
