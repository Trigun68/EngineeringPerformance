import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Parametric norms', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('IIC norms page loads and shows reference grid', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await expect(page.locator('.ag-root-wrapper')).toBeVisible({ timeout: 15_000 });
  });

  test('create dialog lists Kim curve formula type', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await expect(page.locator('.ag-root-wrapper')).toBeVisible({ timeout: 15_000 });
    await page.getByRole('button', { name: 'Добавить' }).click();
    await expect(page.getByText('Новая норма')).toBeVisible();
    await page.getByLabel('Тип формулы').click();
    await expect(page.getByRole('option', { name: 'Кривая Кима' })).toBeVisible();
    await page.keyboard.press('Escape');
    await page.getByRole('button', { name: 'Отмена' }).click();
  });
});
