import { test, expect, type Page } from '@playwright/test';
import { loginAsAdmin, waitForWorkingOverlayHidden } from './helpers';

test.describe('Forecast module - WPF Style', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  async function openProjectForecast(page: Page) {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);
    await waitForWorkingOverlayHidden(page);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
  }

  test('forecast tab shows version button and import buttons', async ({ page }) => {
    await openProjectForecast(page);

    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить оборуд.' })).toBeVisible();
  });

  test('forecast tab has sub-tabs: Итого, Итого по объектам, Детально', async ({ page }) => {
    await openProjectForecast(page);

    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('forecast summary tab shows export buttons', async ({ page }) => {
    await openProjectForecast(page);
    await page.getByRole('tab', { name: 'Итого' }).first().click();
    await expect(page.getByRole('button', { name: /Прогноз$/ })).toBeVisible();
  });

  test('forecast report dialog opens from Прогноз button', async ({ page }) => {
    await openProjectForecast(page);
    await page.getByRole('button', { name: /Прогноз$/ }).click();
    await expect(page.getByTestId('forecast-report-dialog')).toBeVisible();
  });

  test('planning parameters dialog shows Z-parameters caption', async ({ page }) => {
    await openProjectForecast(page);
    await page.getByRole('button', { name: /^Параметры$/i }).click();
    await expect(page.getByTestId('planning-parameters-dialog')).toBeVisible();
    await expect(
      page.getByTestId('z-parameters-caption').getByText(
        /Z-параметры сценариев по группам физобъёмов IIC для выбранной версии плана/,
      ),
    ).toBeVisible();
    await expect(page.getByTestId('z-parameter-level-selector')).toBeVisible();
    await expect(page.getByTestId('z-project-row')).toBeVisible();
    // На прогнозе колонки «Факт» нет (она только на оценке факта).
    await expect(
      page.getByTestId('planning-parameters-dialog').getByRole('columnheader', { name: 'Факт' }),
    ).toHaveCount(0);

    await page.getByLabel('Уровень параметров Z-оценки').click();
    await page.getByRole('option', { name: 'По проекту' }).click();
    await expect(page.getByTestId('z-project-row')).toBeVisible();
    // При «По проекту» строки направлений скрыты — остаётся только строка проекта.
    await expect(
      page.getByTestId('planning-parameters-dialog').locator('tbody tr'),
    ).toHaveCount(1);
  });

  test('forecast detail tab shows export actions after version select', async ({ page }) => {
    await openProjectForecast(page);

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
    await waitForWorkingOverlayHidden(page);

    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.getByRole('button', { name: 'Экспорт всех excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт выделенных excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сводная таблица' })).toBeVisible();
  });

  test('import FO enabled after plan version selected', async ({ page }) => {
    await openProjectForecast(page);

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
    await waitForWorkingOverlayHidden(page);

    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeEnabled({ timeout: 15_000 });
  });
});
