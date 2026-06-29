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

  test('forecast tab has sub-tabs: Итого, По объектам, Детально', async ({ page }) => {
    // Navigate to a project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project to open it
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click on Прогнозирование tab
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    // Check for sub-tabs
    await expect(page.getByRole('tab', { name: 'Итого' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'По объектам' })).toBeVisible();
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

  test('forecast facility tab shows export buttons', async ({ page }) => {
    // Navigate to a project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project to open it
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click on Прогнозирование tab
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();

    // Click on По объектам sub-tab
    const facilityTab = page.getByRole('tab', { name: 'По объектам' }).first();
    await facilityTab.click();

    // Check for export buttons
    await expect(page.getByRole('button', { name: 'Экспорт Excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
  });
});
