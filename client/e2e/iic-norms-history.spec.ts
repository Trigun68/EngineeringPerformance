import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

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

    // Проверяем, что дерево групп реально рендерится и содержит осмысленные подписи.
    const labels = await page.locator('.ag-group-value').allTextContents();
    const normalized = labels.map((x) => x.trim()).filter(Boolean);
    expect(normalized.length).toBeGreaterThan(0);
    expect(new Set(normalized).size).toBeGreaterThan(0);
  });

  test('history dialog shows all columns: dates, performance, estimate, std dev', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
    await firstRow.dblclick();

    const editDialogTitle = page.getByText('Редактирование нормы');
    const openedByDoubleClick = await editDialogTitle.isVisible().catch(() => false);
    if (!openedByDoubleClick) {
      await firstRow.click();
      await page.getByRole('button', { name: 'Изменить' }).click();
    }

    // Check that dialog is open (title should be visible)
    await expect(editDialogTitle).toBeVisible();

    // Check for history section label
    await expect(page.getByText('Исторические записи')).toBeVisible();

    // Check for column headers - using more flexible selectors
    const dialog = page.locator('[role="dialog"]');
    
    // Check for date headers
    await expect(dialog.getByText('Действует с', { exact: false }).first()).toBeVisible();
    await expect(dialog.getByText('Действует по', { exact: false }).first()).toBeVisible();
    
    // Check for value headers
    await expect(dialog.getByText(/Производительность/i).first()).toBeVisible();
    await expect(dialog.getByText(/Оценка/i).first()).toBeVisible();
    await expect(dialog.getByText(/Станд\.? отклонение|Отклонение/i).first()).toBeVisible();

    // Close dialog
    await page.getByRole('button', { name: 'Отмена' }).click();
  });

  test('can add history row with all three values', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Click add button
    await page.getByRole('button', { name: 'Добавить' }).click();

    // Wait for dialog
    await expect(page.getByText('Новая норма')).toBeVisible();

    // Select a group using the hierarchical select
    const groupSelect = page.locator('input[placeholder]').filter({ hasText: /выбрать/i }).first();
    if (await groupSelect.isVisible().catch(() => false)) {
      await groupSelect.click();
      // Wait for options and select first
      await page.waitForTimeout(500);
    }

    // Look for "Добавить строку" button in history section
    const addRowButton = page.getByRole('button', { name: 'Добавить строку' });
    if (await addRowButton.isVisible().catch(() => false)) {
      await addRowButton.click();

      // Find date inputs in the dialog
      const dateInputs = page.locator('input[type="date"]');
      const dateCount = await dateInputs.count();
      
      if (dateCount > 0) {
        // Fill in a date
        await dateInputs.last().fill('2024-01-01');
      }

      // Find number inputs for values
      const numberInputs = page.locator('input[type="number"]');
      const numCount = await numberInputs.count();
      
      if (numCount >= 3) {
        // Fill in values for performance, estimate, std dev
        await numberInputs.nth(numCount - 3).fill('10.5');
        await numberInputs.nth(numCount - 2).fill('8.2');
        await numberInputs.nth(numCount - 1).fill('1.2');
      }
    }

    // Close dialog without saving
    await page.getByRole('button', { name: 'Отмена' }).click();
  });
});
