import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('BER physical volumes - classifier lookup', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('BER edit dialog shows IIC classifier lookup', async ({ page }) => {
    await page.goto('/references/physical-volumes-ber');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    await page.getByRole('button', { name: 'Добавить' }).click();
    await expect(page.getByText('Новая запись')).toBeVisible();

    // FIX-03: поле «Номенклатура КФО» удалено; остаётся классификатор ИИС.
    await expect(page.getByText(/Классификатор \(КФО/i).first()).toBeVisible();
    await expect(page.getByText(/Номенклатура физ\.объёмов КФО/i)).toHaveCount(0);

    await page.getByRole('button', { name: 'Отмена' }).click();
  });
});
