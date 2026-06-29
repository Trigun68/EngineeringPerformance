import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Dictionary persistence', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('physical volumes KFO grid loads and toolbar is visible', async ({ page }) => {
    await page.goto('/references/physical-volumes-kfo');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    await expect(page.getByRole('button', { name: 'Обновить' })).toBeVisible();
    await expect(page.getByText('Показать архивные')).toBeVisible();
  });

  test('equipment directory opens hierarchical editor', async ({ page }) => {
    await page.goto('/references/equipment');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    await page.getByRole('button', { name: 'Добавить' }).click();
    await expect(page.getByText('Новая запись')).toBeVisible();
    await expect(page.getByRole('dialog').getByText('Наименование')).toBeVisible();
    await expect(page.getByRole('dialog').getByText('Родитель')).toBeVisible();
  });
});
