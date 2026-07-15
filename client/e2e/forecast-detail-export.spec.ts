import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Forecast detail export buttons', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('detail tab shows excel, pdf and summary table buttons in tabs row', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    await page.getByRole('button', { name: /Версия \(план\)/ }).click();
    const dialog = page.getByTestId('version-picker-dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });
    const versionRow = dialog.locator('.ag-center-cols-container .ag-row').first();
    if ((await versionRow.count()) === 0) {
      test.skip(true, 'No analysis versions in seed data');
      return;
    }
    await versionRow.click();
    await dialog.getByRole('button', { name: 'Выбрать' }).click();

    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.getByRole('button', { name: 'Экспорт Excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сводная таблица' })).toBeVisible();
  });
});
