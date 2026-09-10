import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Labor IIC binding history', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('double-click opens binding history', async ({ page }) => {
    await page.goto('/references/labor-intensities');
    await expect(page).toHaveURL(/\/references\/labor-intensities/, { timeout: 20_000 });
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 20_000 });

    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
    await firstRow.dblclick();

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });
    await expect(dialog.getByRole('button', { name: 'Добавить' })).toBeVisible();
    await expect(dialog.getByRole('columnheader', { name: 'Действует с' })).toBeVisible();
    const empty = dialog.getByText('Записей нет', { exact: true });
    const dataRows = dialog.locator('tbody tr');
    if ((await empty.count()) > 0) {
      await expect(empty).toBeVisible();
    } else {
      await expect(dataRows.first()).toBeVisible();
    }
  });
});
