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
    await expect(page.getByTestId('analytics-period-select')).toBeVisible();
    await expect(page.getByTestId('fact-norm-accuracy-badge')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('fact-norm-kpi')).toBeVisible();
    await expect(page.getByTestId('fact-norm-project-scope')).toBeVisible();

    await page.getByTestId('fact-norm-project-scope').click();
    await expect(page.getByTestId('project-picker-dialog')).toBeVisible();
    await page.getByTestId('project-picker-filter').fill('zzz-nonexistent-filter');
    await page.getByTestId('project-picker-all').click();
    await expect(page.getByTestId('project-picker-dialog')).toHaveCount(0);
  });

  test('table section and fact vs norm chart area are present', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-fact-norms').click();
    await expect(page.getByTestId('fact-norm-analysis-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByText('Сравнение по группам IIC')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('Серьёзность')).toBeVisible();
    await expect(
      page.getByTestId('fact-vs-norm-chart').or(page.getByTestId('fact-vs-norm-empty')).or(page.getByTestId('fact-norm-empty')),
    ).toBeVisible({ timeout: 60_000 });
  });
});
