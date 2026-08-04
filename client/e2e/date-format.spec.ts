import { test, expect } from '@playwright/test';
import { clearMatchingGridLayouts, loginAsAdmin, scrollAgGridToColumn } from './helpers';

test.describe('Date/time formatting', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('system logs show datetime in YYYY-MM-DD HH:mm:ss format', async ({ page }) => {
    await page.goto('/admin/logs');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    
    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
    const timeCell = firstRow.locator('.ag-cell[col-id="timestampUtc"]');
    const timeText = await timeCell.textContent();
    
    // Check format: YYYY-MM-DD HH:mm:ss
    expect(timeText).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/);
  });

  test('config audit shows datetime in YYYY-MM-DD HH:mm:ss format', async ({ page }) => {
    await page.goto('/admin/audit');
    await page.waitForSelector('[data-testid="admin-config-audit-grid"] .ag-row', { timeout: 15_000 });

    const firstRow = page.locator('[data-testid="admin-config-audit-grid"] .ag-center-cols-container .ag-row').first();
    const timeCell = firstRow.locator('.ag-cell[col-id="timestampUtc"]');
    const timeText = await timeCell.textContent();

    if (timeText && timeText.trim()) {
      expect(timeText).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/);
    }
  });

  test('user action log shows datetime in YYYY-MM-DD HH:mm:ss format', async ({ page }) => {
    await page.goto('/admin/user-actions');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    
    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
    const timeCell = firstRow.locator('.ag-cell[col-id="timestamp"]');
    const timeText = await timeCell.textContent();
    
    if (timeText && timeText.trim()) {
      expect(timeText).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/);
    }
  });

  test('IIC norms effectiveFrom shows date only in YYYY-MM-DD format', async ({ page }) => {
    await clearMatchingGridLayouts(page, ['ref-iic-norms', 'grid-ref-iic-norms']);
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-root-wrapper', { timeout: 20_000 });
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 20_000 });

    // Гарантируем видимость колонки через настройки (prefs могли её спрятать).
    await page.getByRole('button', { name: 'Настройка столбцов' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    const dateCheckbox = dialog.getByRole('checkbox', { name: 'Действует с' });
    await expect(dateCheckbox).toBeVisible({ timeout: 10_000 });
    if (!(await dateCheckbox.isChecked())) {
      await dateCheckbox.check();
    }
    await dialog.getByRole('button', { name: 'Применить' }).click();

    await scrollAgGridToColumn(page, 'effectiveFrom', 'ref-iic-norms');
    const dateCells = page.locator('.ag-center-cols-container .ag-cell[col-id="effectiveFrom"]');
    await expect(dateCells.first()).toBeVisible({ timeout: 20_000 });

    const count = await dateCells.count();
    let checked = 0;
    for (let i = 0; i < count && checked < 5; i++) {
      const dateText = (await dateCells.nth(i).textContent())?.trim() ?? '';
      if (!dateText) continue;
      expect(dateText).toMatch(/^\d{4}-\d{2}-\d{2}$/);
      checked += 1;
    }
    expect(checked, 'expected at least one non-empty effectiveFrom cell').toBeGreaterThan(0);
  });
});
