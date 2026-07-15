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

  test('reports tab has combined PDF button', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Отчёты' }).click();
    await expect(page.getByRole('button', { name: 'Скачать сводный PDF' })).toBeVisible();
  });

  test('trends page has PDF export button', async ({ page }) => {
    await page.goto('/analytics/trends');
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible({ timeout: 15_000 });
  });
});
