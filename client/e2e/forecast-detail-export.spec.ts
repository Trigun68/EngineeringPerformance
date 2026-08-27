import { expect, test } from '@playwright/test';
import { loginAsAdmin, waitForWorkingOverlayHidden, actionButton } from './helpers';

test.describe('Forecast detail export buttons', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('detail tab shows excel buttons and no pdf button in tabs row', async ({ page }) => {
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
    await expect(actionButton(page, 'exportExcel')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSelected')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSummary')).toBeVisible();
    // PDF детализации отключён: полная таблица на десятки тысяч строк даёт непригодный файл.
    await expect(actionButton(page, 'exportPdf')).toHaveCount(0);
  });

  test('detail Excel returns spreadsheet with Russian filename', async ({ page }) => {
    test.setTimeout(180_000);
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
    await waitForWorkingOverlayHidden(page);

    await page.getByRole('tab', { name: 'Детально' }).click();
    await waitForWorkingOverlayHidden(page);
    const excelButton = actionButton(page, 'exportExcel');
    await expect(excelButton).toBeEnabled({ timeout: 30_000 });

    const downloadPromise = page.waitForEvent('download', { timeout: 120_000 });
    await excelButton.click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^прогноз_детально_-_.+\(.+\)\.xlsx$/);
  });
});
