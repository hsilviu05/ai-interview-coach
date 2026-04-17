import { expect, type Page } from '@playwright/test';

export async function getEditorValue(page: Page): Promise<string> {
  const hiddenTextarea = page.getByTestId('code-editor-value');
  return hiddenTextarea.inputValue();
}

export async function replaceEditorValue(
  page: Page,
  nextValue: string
): Promise<void> {
  const editorShell = page.getByTestId('code-editor');
  await expect(editorShell).toBeAttached();

  const fallbackEditor = page.getByTestId('code-editor-textarea');
  const monacoEditor = page.locator('.monaco-editor').first();

  const winner = await Promise.race([
    fallbackEditor.waitFor({ state: 'visible', timeout: 15000 }).then(() => 'fallback' as const),
    monacoEditor.waitFor({ state: 'visible', timeout: 15000 }).then(() => 'monaco' as const),
  ]);

  if (winner === 'fallback') {
    await fallbackEditor.fill(nextValue);
    return;
  }

  await monacoEditor.click();
  await page.keyboard.press('ControlOrMeta+A');
  await page.keyboard.press('Backspace');
  await page.keyboard.type(nextValue);
}
