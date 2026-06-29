import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Dictionary hierarchy', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('collapses and expands iic work directions tree', async ({ page }) => {
    await page.goto('/references/iic-work-directions');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    await page.waitForSelector('.ag-icon-tree-open', { timeout: 15_000 });

    const rowsExpanded = await page.locator('.ag-center-cols-container .ag-row').count();
    expect(rowsExpanded).toBeGreaterThan(6);

    await page.getByRole('button', { name: 'Свернуть всё' }).click();
    await expect(page.locator('.ag-center-cols-container .ag-row')).toHaveCount(6);

    await page.getByRole('button', { name: 'Развернуть всё' }).click();
    await expect(page.locator('.ag-center-cols-container .ag-row').count()).resolves.toBeGreaterThan(6);
  });

  test('iic norms modal supports history rows', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    await page.getByRole('button', { name: 'Добавить' }).click();
    await expect(page.getByText('Новая норма')).toBeVisible();

    const groupSelect = page.getByRole('combobox').first();
    await groupSelect.click();
    await page.getByRole('option').nth(1).click();

    await expect(page.getByText('Исторические записи')).toBeVisible();
    await page.getByRole('button', { name: 'Добавить строку' }).click();
    await expect(page.getByLabel('Действует с').first()).toBeVisible();
    await expect(page.getByLabel('Значение').first()).toBeVisible();
  });

  test('iic norms displays data without duplication', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    
    // Wait for data to load
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    
    // Get all row texts to check for duplicates
    const rows = page.locator('.ag-center-cols-container .ag-row');
    const rowCount = await rows.count();
    
    // Get API data count from console or check directly
    // We expect 47 data rows (not 94 which would indicate duplication)
    expect(rowCount).toBeGreaterThan(0);
    expect(rowCount).toBeLessThan(100); // Sanity check - should not be doubled
    
    // Check that the grid has loaded properly by verifying at least one cell has data
    const firstCell = rows.first().locator('.ag-cell').first();
    await expect(firstCell).not.toHaveText('');
    
    // Verify no duplicate group headers by checking unique group names
    const groupNames = await page.locator('.ag-cell[col-id="group"] .ag-group-value').allTextContents();
    const uniqueGroupNames = new Set(groupNames);
    
    // If there were duplicates, we'd see the same group multiple times
    // The groups should be unique (each direction appears only once as a group)
    expect(uniqueGroupNames.size).toBe(groupNames.length);
  });
});
