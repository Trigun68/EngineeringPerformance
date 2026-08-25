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

    // SoT: formLabel BER = «Классификатор (группы физ. объёмов / нормы IIC)»
    await expect(page.getByText(/Классификатор \(группы/i).first()).toBeVisible();
    // Старое FIX-03 имя поля удалено; связь КФО остаётся как «Связанная номенклатура КФО».
    await expect(page.getByText(/Номенклатура физ\.объёмов КФО/i)).toHaveCount(0);
    await expect(page.getByText(/Связанная номенклатура КФО/i)).toBeVisible();

    await page.getByRole('button', { name: 'Отмена' }).click();
  });
});
