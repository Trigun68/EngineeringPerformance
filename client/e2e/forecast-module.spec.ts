import { test, expect, type Page } from '@playwright/test';
import { loginAsAdmin, openPreferredProject, waitForWorkingOverlayHidden, actionButton, ACTION_LABELS } from './helpers';

test.describe('Forecast module - WPF Style', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  async function openProjectForecast(page: Page) {
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await waitForWorkingOverlayHidden(page);
  }

  test('forecast tab shows version button and import buttons', async ({ page }) => {
    await openProjectForecast(page);

    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible();
    await expect(actionButton(page, 'importPhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'deletePhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'importEquipment')).toBeVisible();
    await expect(actionButton(page, 'deleteEquipment')).toBeVisible();
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
    await expect(page.getByTestId('forecast-summary-export-excel')).toBeVisible();
    await expect(page.getByTestId('forecast-summary-export-pdf')).toBeVisible();
  });

  test('forecast summary excel uses Russian filename template', async ({ page }) => {
    test.setTimeout(90_000);
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

    await page.getByRole('tab', { name: 'Итого' }).first().click();
    const downloadPromise = page.waitForEvent('download', { timeout: 60_000 });
    await page.getByTestId('forecast-summary-export-excel').click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^прогноз_итого_-_.+\(.+\)\.xlsx$/);
  });

  test('forecast facility tab shows matching excel and pdf export buttons', async ({ page }) => {
    await openProjectForecast(page);
    await page.getByRole('tab', { name: 'Итого по объектам' }).click();
    await expect(page.getByTestId('forecast-facility-export-excel')).toBeVisible();
    await expect(page.getByTestId('forecast-facility-export-pdf')).toBeVisible();
  });

  test('forecast report dialog opens from Прогноз button', async ({ page }) => {
    await openProjectForecast(page);
    await page.getByRole('button', { name: /Прогноз$/ }).click();
    const report = page.getByTestId('forecast-report-dialog');
    await expect(report).toBeVisible();
    await expect(report.getByText(/Прогноз трудозатрат:/)).toBeVisible();
    await expect(report.getByRole('button', { name: ACTION_LABELS.exportExcel, exact: true })).toBeVisible();
    await expect(report.getByRole('button', { name: ACTION_LABELS.exportPdf, exact: true })).toBeVisible();
  });

  test('forecast report IIC-1 card shows own FO only, not descendant rows', async ({ page }) => {
    test.setTimeout(90_000);
    await openProjectForecast(page);

    await page.getByRole('button', { name: /Версия \(план\)/ }).click();
    const picker = page.getByTestId('version-picker-dialog');
    await expect(picker).toBeVisible({ timeout: 10_000 });
    const versionRow = picker.locator('.ag-center-cols-container .ag-row').first();
    if ((await versionRow.count()) === 0) {
      test.skip(true, 'No plan versions in seed data');
      return;
    }
    await versionRow.click();
    await picker.getByRole('button', { name: 'Выбрать' }).click();
    await expect(picker).toBeHidden({ timeout: 10_000 });
    await waitForWorkingOverlayHidden(page);

    await page.getByRole('button', { name: /Прогноз$/ }).click();
    const report = page.getByTestId('forecast-report-dialog');
    await expect(report).toBeVisible();
    await expect(report.getByRole('button', { name: ACTION_LABELS.refresh, exact: true })).toBeEnabled({
      timeout: 30_000,
    });

    const parentCard = report
      .locator('[data-testid^="forecast-report-card-"]')
      .filter({ hasText: /Общий бетон/ })
      .first();
    await expect(parentCard).toBeVisible({ timeout: 20_000 });

    const childCard = report
      .locator('[data-testid^="forecast-report-card-"]')
      .filter({ hasText: /Плиты на грунте/ })
      .first();
    if ((await childCard.count()) === 0) {
      return;
    }

    const childVolumeTable = childCard.locator('table').first();
    if ((await childVolumeTable.count()) === 0) {
      return;
    }
    const childCode = (await childVolumeTable.locator('tbody tr td').first().textContent())?.trim();
    if (!childCode) {
      return;
    }
    await expect(parentCard).not.toContainText(childCode);
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
    await expect(actionButton(page, 'exportExcel')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSelected')).toBeVisible();
<<<<<<< HEAD
    await expect(actionButton(page, 'exportPdf')).toBeVisible();
=======
    // PDF детализации отключён: полная таблица на десятки тысяч строк даёт непригодный файл.
    await expect(actionButton(page, 'exportPdf')).toHaveCount(0);
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
    await expect(actionButton(page, 'exportExcelSummary')).toBeVisible();
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

    await expect(actionButton(page, 'importPhysicalVolumes')).toBeEnabled({ timeout: 15_000 });
  });

  test('forecast detail has rows after reload with saved non-primary plan version', async ({ page }) => {
    test.setTimeout(90_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    const otherPlanId = await page.evaluate(async (id) => {
      const res = await fetch(`/api/projects/${id}/versions`, { credentials: 'include' });
      if (!res.ok) return null;
      const versions = (await res.json()) as Array<{
        id: number;
        direction: number;
        isArchived: boolean;
        isPrimary?: boolean;
      }>;
      const plan = versions.filter((v) => v.direction === 1 && !v.isArchived);
      const primary = plan.find((v) => v.isPrimary) ?? plan[0];
      const other = plan.find((v) => v.id !== primary?.id);
      return other?.id ?? null;
    }, projectId);
    test.skip(otherPlanId == null, 'Need two plan versions to reproduce prefs race');

    await page.evaluate(
      async ({ id, versionId }) => {
        await fetch('/api/users/me/preferences', {
          method: 'PUT',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            preferences: {
              [`projects.${id}.forecastSubTab`]: 'forecast-detail',
              [`projects.${id}.planVersionId`]: String(versionId),
            },
          }),
        });
      },
      { id: projectId, versionId: otherPlanId },
    );

    await page.goto(`/projects/${projectId}`);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await waitForWorkingOverlayHidden(page);

    await expect.poll(async () => {
      return page.evaluate(() => {
        const w = window as Window & {
          __epGridApis?: Record<string, { getDisplayedRowCount?: () => number }>;
        };
        return w.__epGridApis?.forecast_details_columns?.getDisplayedRowCount?.() ?? 0;
      });
    }, { timeout: 60_000 }).toBeGreaterThan(0);
  });
});
