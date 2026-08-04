import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

async function openReportTab(page: import('@playwright/test').Page) {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/);

  // Дождаться шапки проекта (workspace загружен), затем вкладку.
  await expect(page.getByRole('button', { name: 'Выбрать проект' })).toBeVisible({ timeout: 20_000 });
  const reportTab = page.getByRole('tab', { name: 'Отчет' });
  await expect(reportTab).toBeVisible({ timeout: 15_000 });
  await reportTab.click();

  const panel = page.getByTestId('performance-report-panel');
  await expect(panel).toBeVisible({ timeout: 40_000 });
  await expect(page.getByRole('button', { name: /План:/ })).toBeVisible({ timeout: 20_000 });
  // Данные отчёта подгрузились — PDF активен.
  await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeEnabled({ timeout: 40_000 });
}

test.describe('Performance report tab', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('shows plan/fact pickers, dual Z cards, scale, PDF', async ({ page }) => {
    await openReportTab(page);

    await expect(page.getByRole('button', { name: /Факт:/ })).toBeVisible();
    await expect(page.getByTestId('report-summary-cards')).toBeVisible();
    await expect(page.getByTestId('report-plan-z')).toBeVisible();
    await expect(page.getByTestId('report-fact-z')).toBeVisible();
    await expect(page.getByTestId('report-z-scale')).toBeVisible();
    await expect(page.getByText('Шкала интерпретации Z-оценки')).toBeVisible();
    await expect(page.getByText('Динамика Z-оценки по версиям')).toBeVisible();
    await expect(page.getByText('Выводы и рекомендации')).toBeVisible();
  });

  test('theme toggle switches dark class on panel', async ({ page }) => {
    await openReportTab(page);
    const panel = page.getByTestId('performance-report-panel');
    await expect(panel).not.toHaveClass(/\bdark\b/);
    await page.getByTestId('performance-report-theme').click();
    await expect(panel).toHaveClass(/dark/);
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
    expect(download.suggestedFilename()).toMatch(/PerformanceReport_/i);
  });
});
