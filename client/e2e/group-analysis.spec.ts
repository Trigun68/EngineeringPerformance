import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Group analysis', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('group tab loads projects table or empty state', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    const groupTab = page.getByRole('tab', { name: /Группа|Групповой/i });
    if (!(await groupTab.isVisible().catch(() => false))) {
      test.skip(true, 'GroupAnalysis tab hidden for role');
      return;
    }

    await groupTab.click();
    await expect(page.getByText(/Групповой анализ/i)).toBeVisible({ timeout: 15_000 });
    await expect(
      page.locator('.ag-root, .MuiAlert-root').first(),
    ).toBeVisible({ timeout: 15_000 });
  });
});
