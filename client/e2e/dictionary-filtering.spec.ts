import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Dictionary filtering', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('excel-style filter shows search, select all and apply', async ({ page }) => {
    await page.goto('/references/units-of-measurement');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    const initialRows = await page.locator('.ag-center-cols-container .ag-row').count();
    expect(initialRows).toBeGreaterThan(0);

    const nameHeader = page.locator('.ag-header-cell').filter({ hasText: 'Наименование' });
    await nameHeader.locator('.ag-header-cell-filter-button').click();

    await expect(page.getByPlaceholder('Поиск...')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Выбрать все' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Снять все' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Применить' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сбросить' })).toBeVisible();

    await page.getByPlaceholder('Поиск...').fill('несуществующее_значение_xyz');
    await expect(page.getByText('Нет совпадений')).toBeVisible();

    await page.getByRole('button', { name: 'Сбросить' }).click();
    await nameHeader.locator('.ag-header-cell-filter-button').click();
    await page.getByRole('button', { name: 'Снять все' }).click();
    await page.getByRole('button', { name: 'Применить' }).click();

    await expect(page.locator('.ag-center-cols-container .ag-row')).toHaveCount(0, { timeout: 10_000 });
  });

  test('column settings dialog lists all configured columns', async ({ page }) => {
    await page.goto('/references/units-of-measurement');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    await page.getByRole('button', { name: 'Настройка столбцов' }).click();
    await expect(page.getByRole('heading', { name: 'Настройка столбцов' })).toBeVisible();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText('Наименование')).toBeVisible();
    await expect(dialog.getByText('Архив')).toBeVisible();
  });
});
