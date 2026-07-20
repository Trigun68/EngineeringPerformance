import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Forecast module - WPF Style', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('forecast tab shows version button and import buttons', async ({ page }) => {
    // Navigate to a project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project to open it
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click on Прогнозирование tab
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    // Check for version button (Версия (план))
    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible();

    // Check for import buttons
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить оборуд.' })).toBeVisible();
  });

  test('forecast tab has sub-tabs: Итого, Итого по объектам, Детально', async ({ page }) => {
    // Navigate to a project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project to open it
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click on Прогнозирование tab
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    // Check for sub-tabs
    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('forecast summary tab shows export buttons', async ({ page }) => {
    // Navigate to a project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project to open it
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click on Прогнозирование tab
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    // Click on Итого sub-tab (should be default, but click to be sure)
    await page.getByRole('tab', { name: 'Итого' }).first().click();

    // Check for Прогноз button
    await expect(page.getByRole('button', { name: /Прогноз$/ })).toBeVisible();
  });

  test('forecast report dialog opens from Прогноз button', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('button', { name: /Прогноз$/ }).click();
    await expect(page.getByTestId('forecast-report-dialog')).toBeVisible();
  });

  test('planning parameters dialog shows Z-parameters caption', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('button', { name: /Параметры планирования/i }).click();
    await expect(page.getByTestId('planning-parameters-dialog')).toBeVisible();
    await expect(
      page.getByTestId('z-parameters-caption').getByText(
        /Z-параметры сценариев по группам физобъёмов IIC для выбранной версии плана/,
      ),
    ).toBeVisible();
  });

  test('forecast detail tab shows export actions after version select', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    await page.getByRole('button', { name: /Версия \(план\)/ }).click();
    const dialog = page.getByTestId('version-picker-dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });
    const versionRow = dialog.locator('.ag-center-cols-container .ag-row').first();
    if ((await versionRow.count()) === 0) {
      test.skip(true, 'No plan versions in seed data');
      return;
    }
    await versionRow.click();
    await dialog.getByRole('button', { name: 'Выбрать' }).click();
    await expect(dialog).toBeHidden({ timeout: 10_000 });

    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.getByRole('button', { name: 'Экспорт Excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сводная таблица' })).toBeVisible();
  });

  test('import FO enabled after plan version selected', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    await page.getByRole('button', { name: /Версия \(план\)/ }).click();
    const dialog = page.getByTestId('version-picker-dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });
    const versionRow = dialog.locator('.ag-center-cols-container .ag-row').first();
    if ((await versionRow.count()) === 0) {
      test.skip(true, 'No plan versions in seed data');
      return;
    }
    await versionRow.click();
    await dialog.getByRole('button', { name: 'Выбрать' }).click();
    await expect(dialog).toBeHidden({ timeout: 10_000 });

    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeEnabled({ timeout: 15_000 });
  });
});
