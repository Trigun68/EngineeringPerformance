import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test('project open does not white-screen', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(String(e.stack || e)));

  await loginAsAdmin(page);
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 25_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/, { timeout: 15_000 });

  await expect(page.getByRole('tab', { name: 'Прогнозирование' })).toBeVisible({ timeout: 15_000 });
  await expect(page.getByTestId('z-parameters-caption')).toHaveCount(0);
  expect(errors, `pageerrors: ${errors.join('\n')}`).toEqual([]);
});
