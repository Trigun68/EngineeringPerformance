import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Analytics page — trends', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('trends tab shows detail view with fact/plan toggle and KPI row', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('analytics-tab-trends')).toHaveText('Анализ тренда оценки факта');
    await page.getByTestId('analytics-tab-trends').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
    await expect(page.getByTestId('ztrend-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('ztrend-weight-mode')).toBeVisible();

    await expect(page.getByTestId('analytics-placeholder')).toHaveCount(0);
    await expect(
      page.getByTestId('ztrend-detail')
        .or(page.getByTestId('ztrend-empty'))
        .or(page.getByTestId('ztrend-error')),
    ).toBeVisible({ timeout: 60_000 });

    if (await page.getByTestId('ztrend-detail').isVisible()) {
      await expect(page.getByTestId('ztrend-detail-period-chip')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-fact-plan-toggle')).toBeVisible();
      await expect(page.getByTestId('ztrend-weight-chip')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-kpi')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-kpi-current')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-kpi-delta')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-dynamics-chart')).toBeVisible();
      // Переключатель режимов перестраивает KPI и график.
      await page.getByTestId('ztrend-detail-fact-plan-toggle-both').click();
      await expect(page.getByTestId('ztrend-detail-kpi')).toBeVisible();
      // Секция нормативных режимов сохраняется ниже.
      await expect(page.getByTestId('ztrend-overall-chart')).toBeVisible();
    }
  });

  test('custom period shows date from/to fields', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible({ timeout: 15_000 });

    await page.getByTestId('analytics-period-select').click();
    await page.getByRole('option', { name: 'Произвольный период' }).click();

    await expect(page.getByTestId('analytics-date-from')).toBeVisible();
    await expect(page.getByTestId('analytics-date-to')).toBeVisible();
  });

  test('chrome toggle hides period filters', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible({ timeout: 15_000 });
    await page.getByTestId('analytics-chrome-toggle').click();
    await expect(page.getByTestId('analytics-period-filters')).toHaveCount(0);
    await page.getByTestId('analytics-chrome-toggle').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
  });

  test('versions table is present in the tab body', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-trends').click();
    await expect(
      page
        .getByTestId('ztrend-detail-versions-table')
        .or(page.getByTestId('ztrend-detail-versions-empty'))
        .or(page.getByTestId('ztrend-empty')),
    ).toBeVisible({ timeout: 60_000 });
  });

  test('fact-norms tab keeps the same period filters', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-fact-norms').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
  });

  test('productivity trend tab shows index detail view and norm section', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('analytics-tab-productivity-trend')).toHaveText(
      'Анализ тренда производительности',
    );
    await page.getByTestId('analytics-tab-productivity-trend').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
    await expect(page.getByTestId('prod-trend-volume-mode')).toBeVisible();
    await expect(page.getByTestId('prod-trend-panel')).toBeVisible({ timeout: 20_000 });

    // Первичный блок — индекс производительности (мокап TrendDetailMockup).
    await expect(
      page
        .getByTestId('prod-trend-detail')
        .or(page.getByTestId('prod-index-empty'))
        .or(page.getByTestId('prod-index-error')),
    ).toBeVisible({ timeout: 60_000 });

    if (await page.getByTestId('prod-trend-detail').isVisible()) {
      await expect(page.getByTestId('prod-trend-detail-period-chip')).toBeVisible();
      await expect(page.getByTestId('prod-trend-detail-fact-plan-toggle')).toBeVisible();
      await expect(page.getByTestId('prod-trend-detail-kpi')).toBeVisible();
      await expect(page.getByTestId('prod-trend-detail-dynamics-chart')).toBeVisible();
    }

    // Вторичная секция норм IIC сохраняется.
    await expect(
      page.getByTestId('prod-trend-overall-chart')
        .or(page.getByTestId('prod-trend-empty'))
        .or(page.getByTestId('prod-trend-error')),
    ).toBeVisible({ timeout: 60_000 });

    if (await page.getByTestId('prod-trend-overall-chart').isVisible()) {
      await expect(page.getByTestId('prod-trend-kpi-row')).toBeVisible();
    }
  });

  test('legacy /analytics/trends redirects to /analytics', async ({ page }) => {
    await page.goto('/analytics/trends');
    await expect(page).toHaveURL(/\/analytics\/?$/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
  });
});
