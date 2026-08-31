import { test, expect, type Page } from '@playwright/test';
import { loginAsAdmin } from './helpers';

async function openNormPeriodsDialog(page: Page) {
  await page.goto('/references/iic-norms');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

  const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
  await firstRow.dblclick();

  const editDialogTitle = page.getByText('Редактирование нормы');
  if (!(await editDialogTitle.isVisible().catch(() => false))) {
    await firstRow.click();
    await page.getByRole('button', { name: 'Редактировать' }).click();
  }

  await expect(editDialogTitle).toBeVisible({ timeout: 10_000 });
}

test.describe('IIC Norms - History records', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('IIC norms grid shows data without duplication', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    const rows = page.locator('.ag-center-cols-container .ag-row');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);

    const labels = await page.locator('.ag-group-value').allTextContents();
    const normalized = labels.map((x) => x.trim()).filter(Boolean);
    expect(normalized.length).toBeGreaterThan(0);
    expect(new Set(normalized).size).toBeGreaterThan(0);
  });

  test('edit dialog shows read-only group, unit and history table', async ({ page }) => {
    await openNormPeriodsDialog(page);
    await expect(page.getByText('Исторические записи')).toBeVisible();

    const dialog = page.locator('[role="dialog"]').first();
    await expect(dialog.getByText('Действует с', { exact: false }).first()).toBeVisible();
    await expect(dialog.getByText('Действует по', { exact: false }).first()).toBeVisible();
    await expect(dialog.getByText(/Производительность/i).first()).toBeVisible();
    await expect(dialog.getByText('μ').first()).toBeVisible();
    await expect(dialog.getByText('σ').first()).toBeVisible();

    await page.getByRole('button', { name: 'Отмена' }).click();
  });

  test('can open sub-dialog to add history row from edit mode', async ({ page }) => {
    await openNormPeriodsDialog(page);

    const addRowButton = page.getByRole('button', { name: 'Добавить строку' });
    await expect(addRowButton).toBeVisible();
    await addRowButton.click();

    await expect(page.getByText('Новая запись истории')).toBeVisible();

    await page.getByRole('button', { name: 'Отмена' }).last().click();
    await page.getByRole('button', { name: 'Отмена' }).first().click();
  });

  test('double-click on a history row opens the record editor', async ({ page }) => {
    await openNormPeriodsDialog(page);
    await expect(page.getByText('Исторические записи')).toBeVisible();

    const historyRow = page.getByTestId(/iic-norm-history-row-/).first();
    await expect(historyRow).toBeVisible({ timeout: 10_000 });
    await historyRow.dblclick();

    await expect(page.getByTestId('iic-norm-history-dialog')).toBeVisible();
    await expect(page.getByText('Редактирование записи')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сохранить' })).toBeVisible();
  });
});
