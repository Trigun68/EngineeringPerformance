import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Project workspace chrome', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  async function openProject(page: import('@playwright/test').Page) {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    const preferred = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: '25-310' });
    if ((await preferred.count()) > 0) {
      await preferred.first().dblclick();
    } else {
      await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    }
    await page.waitForURL(/\/projects\/\d+/);
  }

  test('collapses chrome including project actions and main tabs', async ({ page }) => {
    await openProject(page);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    const toggle = page.getByTestId('project-header-toggle');
    await expect(toggle).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('workspace-chrome-toggle-row')).toBeVisible();

    await expect(page.getByRole('button', { name: 'Выбрать проект' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Основные данные' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();

    await toggle.click();

    await expect(page.getByTestId('workspace-compact-bar')).toBeVisible();
    await expect(page.getByTestId('workspace-compact-project')).toBeVisible();
    await expect(page.getByTestId('workspace-compact-tab')).toHaveText('Прогнозирование');
    // Кнопка — отдельной строкой перед таблицей, не сбоку в compact-bar.
    await expect(page.getByTestId('workspace-chrome-toggle-row')).toBeVisible();
    await expect(page.getByTestId('project-header-toggle')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Выбрать проект' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Основные данные' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Характеристики' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Участники' })).toHaveCount(0);
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toHaveCount(0);
    await expect(page.getByTestId('datatable-expand-all')).toHaveCount(0);

    await toggle.click();
    await expect(page.getByRole('button', { name: 'Основные данные' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();
  });

  test('forecast sub-tabs use plural objects label and detail', async ({ page }) => {
    await openProject(page);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });
});
