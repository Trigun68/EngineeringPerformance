import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

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
    await page.goto('/admin/config-audit');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    
    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
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
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    
    // Look for a leaf row with effectiveFrom
    const rows = page.locator('.ag-center-cols-container .ag-row');
    const count = await rows.count();
    
    for (let i = 0; i < Math.min(count, 5); i++) {
      const dateCell = rows.nth(i).locator('.ag-cell[col-id="effectiveFrom"]');
      const dateText = await dateCell.textContent();
      
      if (dateText && dateText.trim()) {
        // Should be YYYY-MM-DD format, no time
        expect(dateText).toMatch(/^\d{4}-\d{2}-\d{2}$/);
      }
    }
  });
});
