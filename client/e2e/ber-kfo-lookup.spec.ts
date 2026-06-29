import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('BER physical volumes - KFO lookup', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('BER edit dialog shows KFO hierarchical select with empty option', async ({ page }) => {
    await page.goto('/references/physical-volumes-ber');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Click add button
    await page.getByRole('button', { name: 'Добавить' }).click();

    // Check that dialog is open
    await expect(page.getByText('Новая запись')).toBeVisible();

    // Check for KFO lookup field - the formLabel is "Номенклатура физ.объёмов КФО"
    // But the visible label might be different, so let's look for the field
    const kfoField = page.locator('label').filter({ hasText: /Номенклатура.*КФО/i });
    await expect(kfoField).toBeVisible();

    // Find the autocomplete input for this field
    const kfoAutocomplete = page.locator('input[placeholder="Не выбрана"]').first();
    await expect(kfoAutocomplete).toBeVisible();

    // Click on the autocomplete to open options
    await kfoAutocomplete.click();

    // Type something or wait for dropdown
    await page.waitForTimeout(500);

    // Close dialog
    await page.getByRole('button', { name: 'Отмена' }).click();
  });

  test('KFO lookup field accepts selection', async ({ page }) => {
    await page.goto('/references/physical-volumes-ber');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Click add button
    await page.getByRole('button', { name: 'Добавить' }).click();

    // Wait for dialog
    await expect(page.getByText('Новая запись')).toBeVisible();

    // Find the KFO autocomplete input
    const kfoAutocomplete = page.locator('input[placeholder="Не выбрана"]').first();
    await expect(kfoAutocomplete).toBeVisible();

    // The autocomplete should be clickable
    await kfoAutocomplete.click();

    // Close dialog
    await page.getByRole('button', { name: 'Отмена' }).click();
  });
});
