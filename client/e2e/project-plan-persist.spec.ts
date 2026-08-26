import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Project and plan persistence', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('restores selected plan version after reload', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    const preferred = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: '25-310' });
    if ((await preferred.count()) > 0) {
      await preferred.first().dblclick();
    } else {
      await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    }
    await page.waitForURL(/\/projects\/\d+/);
    const projectPath = new URL(page.url()).pathname;

    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('button', { name: /Версия \(план\)/ }).click();
    const dialog = page.getByTestId('version-picker-dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });

    const rows = dialog.locator('.ag-center-cols-container .ag-row');
    const rowCount = await rows.count();
    if (rowCount === 0) {
      test.skip(true, 'No plan versions in seed data');
      return;
    }

    const targetRow = rows.nth(Math.max(0, rowCount - 1));
    await targetRow.click();
    await dialog.getByRole('button', { name: 'Выбрать' }).click();
    await expect(dialog).toBeHidden({ timeout: 10_000 });

    const label = page.getByTestId('plan-version-label');
    await expect(label).toBeVisible({ timeout: 10_000 });
    const savedLabel = (await label.innerText()).trim();
    expect(savedLabel.length).toBeGreaterThan(0);

    // Дожидаемся putUserPreferences (planVersionId).
    await page.waitForResponse(
      (r) => r.url().includes('/preferences') && r.request().method() === 'PUT',
      { timeout: 10_000 },
    ).catch(() => undefined);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await page.waitForURL(new RegExp(`${projectPath.replace(/\//g, '\\/')}$`));
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    await expect(page.getByTestId('plan-version-label')).toHaveText(savedLabel, { timeout: 20_000 });
  });

  test('keeps project workspace URL after reload', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    const path = new URL(page.url()).pathname;
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(new RegExp(`${path.replace(/\//g, '\\/')}$`));
    await expect(page.getByRole('button', { name: 'Выбрать проект' })).toBeVisible({ timeout: 15_000 });
  });
});
