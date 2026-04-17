import { expect, type Page } from '@playwright/test';

export async function replaceEditorValue(
  page: Page,
  nextValue: string
): Promise<void> {
  const editorShell = page.getByTestId('code-editor');
  await expect(editorShell).toBeAttached();

  const fallbackEditor = page.getByTestId('code-editor-textarea');
  if (await fallbackEditor.isVisible()) {
    await fallbackEditor.fill(nextValue);
    return;
  }

  const monacoEditor = page.locator('.monaco-editor').first();
  await expect(monacoEditor).toBeVisible({ timeout: 15000 }); 
  
  await monacoEditor.click();
  await page.keyboard.press('ControlOrMeta+A');
  await page.keyboard.press('Backspace');
  await page.keyboard.type(nextValue);
}
