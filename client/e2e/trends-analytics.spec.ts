import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Trends analytics page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('loads trends page with charts', async ({ page }) => {
    await page.goto('/analytics/trends');
    await expect(page.getByRole('heading', { name: /Трендовый анализ/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForSelector('.highcharts-container', { timeout: 30_000 });
    await expect(page.locator('.highcharts-container').first()).toBeVisible();
  });
});
