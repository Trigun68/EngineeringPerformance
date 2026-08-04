import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

async function openFirstProject(page: import('@playwright/test').Page) {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/);
}

test.describe('PDF visual reports export', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('compare tab has PDF export button', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await expect(page.getByRole('button', { name: /Версия А/ })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible({ timeout: 15_000 });
  });

  test('report tab has PDF export for performance report', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Отчет' }).click();
    await expect(page.getByTestId('performance-report-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
  });

  test('analytics page shell is reachable (charts deferred)', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('analytics-main-tabs')).toBeVisible();
  });
});
