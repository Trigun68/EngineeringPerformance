import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Norm analysis registry', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('norm-analysis tab shows summary, timeline and table', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('norm-analysis-filters')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-health-summary')).toBeVisible({ timeout: 60_000 });
    await expect(page.getByTestId('norm-analysis-badge-errors')).toBeVisible();
    await expect(
      page.getByTestId('norm-analysis-timeline').or(page.getByTestId('norm-analysis-timeline-empty')),
    ).toBeVisible();
    await expect(
      page.getByTestId('norm-analysis-table').or(page.getByTestId('norm-analysis-empty')),
    ).toBeVisible({ timeout: 60_000 });
  });

  test('only-with-issues filter and card open', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await page.getByTestId('norm-analysis-only-issues').check();
    await expect(
      page.getByTestId('norm-analysis-table').or(page.getByTestId('norm-analysis-empty')),
    ).toBeVisible({ timeout: 60_000 });

    const grid = page.getByTestId('norm-analysis-grid');
    if (await grid.isVisible()) {
      const firstRow = grid.locator('.ag-row').first();
      if (await firstRow.isVisible()) {
        await firstRow.click();
        await expect(page.getByTestId('norm-analysis-card')).toBeVisible({ timeout: 15_000 });
        await expect(page.getByTestId('norm-card-edit')).toBeVisible();
        await page.getByRole('button', { name: 'Закрыть' }).first().click();
      }
    }
  });

  test('export buttons visible for admin', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('norm-analysis-export-xlsx')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-export-csv')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-export-health')).toBeVisible();
  });
});
