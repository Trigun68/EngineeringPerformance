import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Analytics page — trends', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('trends tab shows portfolio blocks, fact/plan toggle and extra accordion', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('analytics-tab-trends')).toHaveText('Тренд оценки');
    await page.getByTestId('analytics-tab-trends').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
    await expect(page.getByTestId('ztrend-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('ztrend-weight-mode')).toBeVisible();

    await expect(page.getByTestId('analytics-placeholder')).toHaveCount(0);
    await expect(
      page.getByTestId('ztrend-period-chip')
        .or(page.getByTestId('ztrend-empty'))
        .or(page.getByTestId('ztrend-error')),
    ).toBeVisible({ timeout: 60_000 });

    if (await page.getByTestId('ztrend-period-chip').isVisible()) {
      await expect(page.getByTestId('ztrend-fact-plan-toggle')).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Динамика Z по датам' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Отклонения по дисциплинам' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Z по группам IIC относительно нормы' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'S-curve часов' })).toBeVisible();
      await expect(page.getByTestId('ztrend-coverage')).toBeVisible();
      await expect(page.getByTestId('ztrend-movers')).toBeVisible();
      await expect(page.getByTestId('ztrend-export-excel')).toBeVisible();
      await expect(page.getByTestId('ztrend-export-pdf')).toBeVisible();
      await expect(page.getByTestId('ztrend-detail-kpi')).toHaveCount(0);
      await expect(page.getByTestId('ztrend-detail-versions-table')).toHaveCount(0);
      await page.getByTestId('ztrend-fact-plan-toggle-both').click();
      await page.getByTestId('ztrend-extra-summary').click();
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

  test('hours bridge or empty state is on the first screen', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-trends').click();
    await expect(
      page
        .getByTestId('ztrend-hours-bridge')
        .or(page.getByText('Нет пары план/факт с объёмом и часами для моста.'))
        .or(page.getByTestId('ztrend-empty')),
    ).toBeVisible({ timeout: 60_000 });
  });

  test('hours-trend tab shows project picker', async ({ page }) => {
    await page.goto('/analytics?tab=hours-trend');
    await expect(page.getByTestId('analytics-tab-hours-trend')).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByTestId('hours-trend-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('hours-trend-project')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('hours-trend-export-pdf')).toBeVisible();
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
      'Тренд оценки производительности',
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

    await expect(page.getByTestId('prod-trend-hours-chapter')).toBeVisible({ timeout: 60_000 });

    // Вторичная секция норм IIC — в «Дополнительно».
    await page.getByTestId('prod-trend-extra-summary').click();
    await expect(
      page.getByTestId('prod-trend-overall-chart')
        .or(page.getByTestId('prod-trend-empty'))
        .or(page.getByTestId('prod-trend-error')),
    ).toBeVisible({ timeout: 60_000 });

    if (await page.getByTestId('prod-trend-overall-chart').isVisible()) {
      await expect(page.getByTestId('prod-trend-kpi-row')).toBeVisible();
    }
  });

  test('legacy /analytics/trends redirects to hours-trend tab', async ({ page }) => {
    await page.goto('/analytics/trends');
    await expect(page).toHaveURL(/tab=hours-trend/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('analytics-tab-hours-trend')).toHaveAttribute('aria-selected', 'true');
  });
});
