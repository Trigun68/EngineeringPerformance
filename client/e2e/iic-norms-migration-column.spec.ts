import { expect, test } from '@playwright/test';
import { clearMatchingGridLayouts, loginAsAdmin, scrollAgGridToColumn } from './helpers';

test.describe('IIC norms migration column visibility', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('migration column can be hidden via column settings', async ({ page }) => {
    await clearMatchingGridLayouts(page, ['ref-iic-norms', 'grid-ref-iic-norms']);
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-root-wrapper', { timeout: 20_000 });

    await scrollAgGridToColumn(page, 'migration', 'ref-iic-norms');
    await expect(page.locator('.ag-header-cell[col-id="migration"]').first()).toBeVisible({
      timeout: 15_000,
    });

    await page.getByRole('button', { name: 'Настройка столбцов' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();

    const migrationCheckbox = dialog.getByRole('checkbox', { name: 'Миграция' });
    await expect(migrationCheckbox).toBeVisible();
    if (await migrationCheckbox.isChecked()) {
      await migrationCheckbox.uncheck();
    }
    await dialog.getByRole('button', { name: 'Применить' }).click();

    await expect(page.locator('.ag-header-cell[col-id="migration"]')).toHaveCount(0, {
      timeout: 10_000,
    });

    // Restore visibility for repeatable runs
    await page.getByRole('button', { name: 'Настройка столбцов' }).click();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('checkbox', { name: 'Миграция' }).check();
    await dialog.getByRole('button', { name: 'Применить' }).click();

    await scrollAgGridToColumn(page, 'migration', 'ref-iic-norms');
    await expect(page.locator('.ag-header-cell[col-id="migration"]').first()).toBeVisible({
      timeout: 10_000,
    });
  });
});
