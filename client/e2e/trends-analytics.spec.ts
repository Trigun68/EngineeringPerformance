import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Analytics page — Z trends', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('trends tab shows panel with weight toggle and plan/fact switches', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
    await page.getByTestId('analytics-tab-trends').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
    await expect(page.getByTestId('ztrend-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('ztrend-weight-mode')).toBeVisible();
    await expect(page.getByTestId('ztrend-toggle-plan')).toBeVisible();
    await expect(page.getByTestId('ztrend-toggle-fact')).toBeVisible();

    await expect(page.getByTestId('analytics-placeholder')).toHaveCount(0);
    await expect(
      page.getByTestId('ztrend-overall-chart')
        .or(page.getByTestId('ztrend-empty'))
        .or(page.getByTestId('ztrend-error')),
    ).toBeVisible({ timeout: 60_000 });
  });

  test('custom period shows date from/to fields', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible({ timeout: 15_000 });

    const customToggle = page.getByTestId('analytics-period-custom');
    if (await customToggle.isVisible()) {
      await customToggle.click();
    } else {
      await page.getByTestId('analytics-period-select').click();
      await page.getByRole('option', { name: 'Произвольный период' }).click();
    }

    await expect(page.getByTestId('analytics-date-from')).toBeVisible();
    await expect(page.getByTestId('analytics-date-to')).toBeVisible();
  });

  test('fact-norms tab keeps the same period filters', async ({ page }) => {
    await page.goto('/analytics');
    await page.getByTestId('analytics-tab-fact-norms').click();
    await expect(page.getByTestId('analytics-period-filters')).toBeVisible();
  });

  test('legacy /analytics/trends redirects to /analytics', async ({ page }) => {
    await page.goto('/analytics/trends');
    await expect(page).toHaveURL(/\/analytics\/?$/);
    await expect(page.getByRole('heading', { name: 'Аналитика' })).toBeVisible({ timeout: 15_000 });
  });
});
