import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Norm analysis registry', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('norm-analysis tab shows period select, summary, timeline and table', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('analytics-period-select')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-health-summary')).toBeVisible({ timeout: 60_000 });
    await expect(page.getByTestId('norm-analysis-badge-errors')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-view-toggle')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-view-table')).toContainText('Таблица');
    await expect(page.getByTestId('norm-analysis-view-heatmap')).toContainText('Тепловая карта');
    await page.getByTestId('norm-analysis-view-heatmap').click();
    await expect(
      page.getByTestId('norm-analysis-heatmap').or(page.getByTestId('norm-analysis-heatmap-empty')),
    ).toBeVisible();
    await page.getByTestId('norm-analysis-view-table').click();
    await expect(
      page.getByTestId('norm-analysis-timeline').or(page.getByTestId('norm-analysis-timeline-empty')),
    ).toBeVisible();
    const timeline = page.getByTestId('norm-analysis-timeline');
    if (await timeline.isVisible()) {
      const groupTestIds = await page.locator('[data-testid^="norm-timeline-group-"]').evaluateAll((els) =>
        els.map((el) => el.getAttribute('data-testid')),
      );
      expect(new Set(groupTestIds).size).toBe(groupTestIds.length);
    }
    await expect(
      page.getByTestId('norm-analysis-table').or(page.getByTestId('norm-analysis-empty')),
    ).toBeVisible({ timeout: 60_000 });
  });

  test('only-with-issues filter and card open', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await page.getByTestId('norm-analysis-filters-extra-summary').click();
    await expect(page.getByTestId('norm-analysis-filters')).toBeVisible();
    await page.getByTestId('norm-analysis-only-issues').check();
    await expect(
      page.getByTestId('norm-analysis-table').or(page.getByTestId('norm-analysis-empty')),
    ).toBeVisible({ timeout: 60_000 });

    const firstRow = page.locator('[data-testid^="norm-analysis-table-row-"]').first();
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await expect(page.getByTestId('norm-analysis-card')).toBeVisible({ timeout: 15_000 });
      await expect(page.getByTestId('norm-card-edit')).toBeVisible();
      await expect(page.getByTestId('norm-analysis-simulator')).toBeVisible();
    }
  });

  test('export buttons visible for admin', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await page.getByTestId('norm-analysis-filters-extra-summary').click();
    await expect(page.getByTestId('norm-analysis-export-xlsx')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('norm-analysis-export-csv')).toBeVisible();
    await expect(page.getByTestId('norm-analysis-export-health')).toBeVisible();
  });

  test('help icon opens the interpretation dialog', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-norm-analysis').click();
    await expect(page.getByTestId('norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await page.getByTestId('norm-analysis-help').click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Справка: анализ норм' })).toBeVisible();
    await expect(page.getByTestId('norm-analysis-help-body')).toContainText('CV');
    await expect(page.getByTestId('norm-analysis-help-body')).toContainText('Достоверность');
  });
});
