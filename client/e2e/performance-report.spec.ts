import { test, expect } from '@playwright/test';
import { loginAsAdmin, actionButton } from './helpers';

async function openReportTab(page: import('@playwright/test').Page) {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/);

  await expect(page.getByRole('button', { name: 'Выбрать проект' })).toBeVisible({ timeout: 20_000 });
  const reportTab = page.getByRole('tab', { name: 'Отчет' });
  await expect(reportTab).toBeVisible({ timeout: 15_000 });
  await reportTab.click();

  // Прежняя сводная живёт на третьей под-вкладке (оставлена для отката).
  const legacyTab = page.getByTestId('report-legacy-tab');
  await expect(legacyTab).toBeVisible({ timeout: 15_000 });
  await legacyTab.click();

  const panel = page.getByTestId('performance-report-panel');
  await expect(panel).toBeVisible({ timeout: 40_000 });
  // Данные отчёта подгрузились — PDF активен.
  await expect(actionButton(page, 'exportPdf')).toBeEnabled({ timeout: 40_000 });
}

test.describe('Performance report tab', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('shows KPI cards, scale, directions tree, insights — no version pickers', async ({ page }) => {
    await openReportTab(page);

    await expect(page.getByRole('button', { name: /План:/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Факт:/ })).toHaveCount(0);

    await expect(page.getByTestId('report-summary-cards')).toBeVisible();
    await expect(page.getByTestId('report-plan-z')).toBeVisible();
    await expect(page.getByTestId('report-fact-z')).toBeVisible();
    await expect(page.getByTestId('report-labor-plan')).toBeVisible();
    await expect(page.getByTestId('report-labor-fact')).toBeVisible();
    await expect(page.getByTestId('report-accuracy')).toBeVisible();
    await expect(page.getByTestId('report-z-scale')).toBeVisible();
    await expect(page.getByText('Шкала интерпретации Z-оценки')).toBeVisible();
    await expect(page.getByText('Динамика Z-оценки по версиям')).toBeVisible();
    await expect(page.getByTestId('report-insights')).toBeVisible();
    await expect(page.getByTestId('report-directions-table')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Направление' })).toBeVisible();
    await expect(page.getByTestId('report-z-tick')).toHaveCount(7);
    const planVersion = page.getByTestId('report-plan-version');
    if ((await planVersion.count()) > 0) {
      await expect(planVersion).toContainText('План:');
    }
  });

  test('theme toggle switches dark class on panel', async ({ page }) => {
    await openReportTab(page);
    const panel = page.getByTestId('performance-report-panel');
    const startedDark = await panel.evaluate((el) => el.classList.contains('dark'));
    await page.getByTestId('performance-report-theme').click();
    if (startedDark) {
      await expect(panel).not.toHaveClass(/\bdark\b/);
    } else {
      await expect(panel).toHaveClass(/dark/);
    }
  });

  test('PDF export returns application/pdf', async ({ page }) => {
    await openReportTab(page);

    const downloadPromise = page.waitForEvent('download', { timeout: 30_000 });
    const responsePromise = page.waitForResponse(
      (r) => r.url().includes('/performance-report/export.pdf') && r.request().method() === 'GET',
      { timeout: 30_000 },
    );
    await page.getByTestId('performance-report-pdf').click();
    const response = await responsePromise;
    expect(response.status()).toBe(200);
    const ct = response.headers()['content-type'] ?? '';
    expect(ct).toContain('application/pdf');
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^отчет_-_.+\.pdf$/);
  });
});
