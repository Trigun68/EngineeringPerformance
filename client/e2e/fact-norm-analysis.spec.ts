import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Fact–norm analysis', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('fact-norms tab shows accuracy badge and project picker', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await page.getByTestId('analytics-tab-fact-norms').click();
    await expect(page.getByTestId('fact-norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('fact-norm-accuracy-badge')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('fact-norm-project-scope')).toBeVisible();

    await page.getByTestId('fact-norm-project-scope').click();
    await expect(page.getByTestId('project-picker-dialog')).toBeVisible();
    await page.getByTestId('project-picker-filter').fill('zzz-nonexistent-filter');
    await page.getByTestId('project-picker-all').click();
    await expect(page.getByTestId('project-picker-dialog')).toHaveCount(0);
  });

  test('short period shows low accuracy hint when applicable', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-fact-norms').click();
    await expect(page.getByTestId('fact-norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    // Default 1m preset → Low accuracy → hint may appear
    const hint = page.getByTestId('fact-norm-low-accuracy-hint');
    const badge = page.getByTestId('fact-norm-accuracy-badge');
    await expect(badge).toBeVisible({ timeout: 30_000 });
    const text = await badge.innerText();
    if (text.includes('Низкая')) {
      await expect(hint).toBeVisible();
    }
  });
});
