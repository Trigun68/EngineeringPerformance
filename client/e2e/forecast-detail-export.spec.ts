import { expect, test } from '@playwright/test';
import { loginAsAdmin, waitForWorkingOverlayHidden, actionButton } from './helpers';

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
    await expect(actionButton(page, 'exportExcel')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSelected')).toBeVisible();
    await expect(actionButton(page, 'exportPdf')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSummary')).toBeVisible();
  });

  test('detail PDF returns application/pdf', async ({ page }) => {
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
    const pdfButton = page.getByTestId('forecast-detail-export-pdf');
    await expect(pdfButton).toBeEnabled({ timeout: 30_000 });

    // Полный PDF детальной таблицы на большом проекте роняет Chromium (blob в вкладке).
    // Имя и тип проверяем по реальному ответу, в страницу отдаём маленькую заглушку.
    let contentType = '';
    let contentDisposition = '';
    await page.route('**/export/forecast-detail/pdf', async (route) => {
      const response = await route.fetch();
      contentType = response.headers()['content-type'] ?? '';
      contentDisposition = response.headers()['content-disposition'] ?? '';
      await route.fulfill({
        status: response.status(),
        contentType: contentType || 'application/pdf',
        headers: {
          'content-disposition': contentDisposition,
        },
        body: Buffer.from('%PDF-1.4 stub'),
      });
    });

    const downloadPromise = page.waitForEvent('download', { timeout: 120_000 });
    await pdfButton.click();
    const download = await downloadPromise;
    expect(contentType).toContain('application/pdf');
    const headerName = filenameFromContentDisposition(contentDisposition);
    expect(headerName).toMatch(/^прогноз_детально_-_.+\(.+\)\.pdf$/);
    expect(download.suggestedFilename()).toMatch(/^прогноз_детально_-_.+\(.+\)\.pdf$/);
  });
});

function filenameFromContentDisposition(header: string): string {
  const utf8 = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (utf8?.[1]) {
    try {
      return decodeURIComponent(utf8[1].replace(/"/g, '').trim());
    } catch {
      return utf8[1].replace(/"/g, '').trim();
    }
  }
  const ascii = /filename="?([^";]+)"?/i.exec(header);
  return ascii?.[1]?.trim() ?? '';
}
