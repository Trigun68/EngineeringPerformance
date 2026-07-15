import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

async function openFirstProject(page: import('@playwright/test').Page) {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/);
}

test.describe('Compare filter behavior', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('show only differences checkbox changes row count', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    const rowsBefore = await page.locator('.ag-center-cols-container .ag-row').count();
    await page.getByLabel('Показывать различия').check();
    await page.waitForTimeout(500);
    const rowsAfter = await page.locator('.ag-center-cols-container .ag-row').count();

    expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
  });
});
