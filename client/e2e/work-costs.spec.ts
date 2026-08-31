import { test, expect } from '@playwright/test';
import { actionButton, loginAsAdmin } from './helpers';

test.describe('Work costs directory', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('tab sits immediately after IIC norms', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await expect(page.getByRole('link', { name: 'Нормы физобъёмов (IIC)' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Стоимость работ' })).toBeVisible();

    const titles = await page.locator('a[href^="/references/"]').allTextContents();
    const normsIdx = titles.findIndex((t) => t.includes('Нормы физобъёмов (IIC)'));
    const costsIdx = titles.findIndex((t) => t.trim() === 'Стоимость работ');
    expect(normsIdx).toBeGreaterThanOrEqual(0);
    expect(costsIdx).toBe(normsIdx + 1);
  });

  test('shows IIC tree, as-of date, set-rate dialog and excel export', async ({ page }) => {
    await page.goto('/references/work-costs');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    await expect(page.getByTestId('work-costs-periods')).toBeVisible();
    await expect(page.getByText('Периоды')).toBeVisible();
    await expect(page.getByLabel('Дата')).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Стоимость, ₽/ч' })).toBeVisible();
    await expect(page.getByTestId('work-costs-periods').getByRole('columnheader', { name: 'Действует с' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Развернуть всё' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Свернуть всё' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Изменить' })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Удалить' })).toBeDisabled();

    await page.getByRole('button', { name: 'Задать стоимость' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText('Задать стоимость')).toBeVisible();
    await expect(dialog.getByLabel('Действует с')).toBeVisible();
    await expect(dialog.getByLabel('Уровень')).toBeVisible();
    await expect(dialog.getByText('В целом по организации')).toBeVisible();
    await dialog.getByRole('button', { name: 'Закрыть', exact: true }).click();

    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 20_000 }),
      actionButton(page, 'exportExcel').click(),
    ]);
    expect(download.suggestedFilename()).toMatch(/справочник_стоимость_работ/);
  });

  test('can create, edit and delete a dated rate block', async ({ page }) => {
    const from = '2098-06-18';
    await page.goto('/references/work-costs');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    await page.getByRole('button', { name: 'Задать стоимость' }).click();
    const dialog = page.getByTestId('work-costs-set-rate-dialog');
    await dialog.getByLabel('Действует с').fill(from);
    await expect(dialog.getByLabel('Действует с')).toHaveValue(from);
    await dialog.getByLabel('Уровень').click();
    await page.getByRole('option', { name: 'В целом по организации' }).click();
    await dialog.getByLabel('Стоимость в целом по организации').fill('1111');
    await dialog.getByRole('button', { name: 'Сохранить' }).click();
    await expect(dialog).toBeHidden({ timeout: 20_000 });

    const periodRow = page.getByTestId(`work-cost-period-${from}`);
    await expect(periodRow).toBeVisible({ timeout: 15_000 });
    await periodRow.click();
    await expect(page.getByText(`Период с ${from}`)).toBeVisible();

    await page.getByRole('button', { name: 'Изменить' }).click();
    await expect(dialog.getByText('Изменить стоимость')).toBeVisible();
    await expect(dialog.getByLabel('Действует с')).toBeDisabled();
    await dialog.getByLabel('Уровень').click();
    await page.getByRole('option', { name: 'В целом по организации' }).click();
    await dialog.getByLabel('Стоимость в целом по организации').fill('2222');
    await dialog.getByRole('button', { name: 'Сохранить' }).click();
    await expect(dialog).toBeHidden({ timeout: 20_000 });
    await expect(page.getByRole('gridcell', { name: /2\s?222/ }).first()).toBeVisible({ timeout: 15_000 });

    await page.getByRole('button', { name: 'Удалить' }).click();
    const confirm = page.getByTestId('confirm-dialog');
    await expect(confirm.getByText('Удалить период?')).toBeVisible();
    await confirm.getByRole('button', { name: 'Удалить' }).click();
    await expect(periodRow).toHaveCount(0, { timeout: 15_000 });
  });
});
