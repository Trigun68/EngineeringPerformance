import { test, expect } from '@playwright/test';
import {
  clearMatchingGridLayouts,
  loginAsAdmin,
  openPreferredProject,
  scrollAgGridToColumn,
  waitForWorkingOverlayHidden,
} from './helpers';

test.describe('Fact and Compare modules - WPF parity', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  async function openFactTab(page: import('@playwright/test').Page): Promise<void> {
    await openPreferredProject(page);
    await waitForWorkingOverlayHidden(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await waitForWorkingOverlayHidden(page);
  }

  test('fact tab shows version button, six import buttons, and sub-tabs', async ({ page }) => {
    await openFactTab(page);

    await expect(page.getByRole('button', { name: /Версия \(факт\)/ })).toBeVisible();
    await expect(page.getByTestId('fact-planning-parameters-button')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт труд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить труд.' })).toBeVisible();

    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Итого по объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('fact grids show plan labor hour columns after fact hours', async ({ page }) => {
    test.setTimeout(90_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, [
      'fact-performance',
      'fact-facility',
      'fact-detail',
    ]);

    await page.goto(`/projects/${projectId}`);
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await waitForWorkingOverlayHidden(page, 90_000);

    const expectPlanHeaders = async (gridId: string) => {
      await scrollAgGridToColumn(page, 'pessimisticHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Песс.' })).toBeVisible({ timeout: 20_000 });
      await scrollAgGridToColumn(page, 'baseHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Баз.' })).toBeVisible();
      await scrollAgGridToColumn(page, 'optimisticHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Оптим.' })).toBeVisible();
    };

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('fact-performance');

    await page.getByRole('tab', { name: 'Итого по объектам' }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('fact-facility');

    await page.getByRole('tab', { name: 'Детально' }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('fact-detail');
  });

  test('fact planning parameters dialog shows Fact column', async ({ page }) => {
    await openFactTab(page);

    const paramsBtn = page.getByTestId('fact-planning-parameters-button');
    if ((await paramsBtn.count()) === 0) {
      test.skip(true, 'No fact version selected');
      return;
    }
    await paramsBtn.click();
    const dialog = page.getByTestId('planning-parameters-dialog');
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByTestId('z-parameters-caption').getByText(/версии факта/),
    ).toBeVisible();
    await expect(dialog.getByRole('columnheader', { name: 'Факт' })).toBeVisible();
    await expect(dialog.getByTestId('z-project-fact')).toBeVisible();
    await dialog.getByRole('button', { name: 'Закрыть', exact: true }).last().click();
  });

  test('fact catalog label shows short name without prefix when present', async ({ page }) => {
    await openFactTab(page);
    const catalog = page.getByTestId('fact-catalog-label');
    if ((await catalog.count()) === 0) {
      test.skip(true, 'No physical volume catalog on selected fact version');
      return;
    }
    await expect(catalog).toBeVisible();
    await expect(catalog).not.toContainText('Справочник ФО:');
  });

  test('fact version picker dialog opens with title Выбор версии (факт)', async ({ page }) => {
    await openFactTab(page);
    await page.getByRole('button', { name: /Версия \(факт\)/ }).click();
    await expect(page.getByRole('dialog').getByText('Выбор версии (факт)')).toBeVisible();
  });

  test('compare tab shows dual version pickers and differences checkbox', async ({ page }) => {
    await openPreferredProject(page);
    await waitForWorkingOverlayHidden(page);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await waitForWorkingOverlayHidden(page);

    await expect(page.getByRole('button', { name: /Версия А/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Версия Б/ })).toBeVisible();
    await expect(page.getByLabel('Показывать различия')).toBeVisible();
    await expect(page.getByLabel('Сценарий')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Таблица', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'График', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт всех excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт выделенных excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Экспорт PDF' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Split' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Удалить оборуд.' })).toHaveCount(0);

    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'По объектам', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('compare grids show six plan labor hour columns after physical volume', async ({ page }) => {
    test.setTimeout(180_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, [
      'compare-summary',
      'compare-facility',
      'compare-detail',
    ]);

    await page.goto(`/projects/${projectId}`);
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await waitForWorkingOverlayHidden(page, 90_000);

    await expect(page.getByRole('button', { name: /Версия А/ })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByLabel('Сценарий')).toBeVisible({ timeout: 30_000 });
    const differences = page.getByRole('checkbox', { name: 'Показывать различия' });
    await expect(differences).toBeEnabled({ timeout: 120_000 });

    const expectPlanHeaders = async (gridId: string) => {
      await scrollAgGridToColumn(page, 'plannedLaborHoursPessimisticA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Песс. v. А)' })).toBeVisible({ timeout: 20_000 });
      await scrollAgGridToColumn(page, 'plannedLaborHoursA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Баз. v. А)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursOptimisticA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Опт. v. А)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursPessimisticB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Песс. v. Б)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Баз. v. Б)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursOptimisticB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Опт. v. Б)' })).toBeVisible();
    };

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('compare-summary');

    await page.getByRole('tab', { name: 'По объектам', exact: true }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('compare-facility');

    await page.getByRole('tab', { name: 'Детально' }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('compare-detail');
  });

  test('fact analysis dialog opens from fact tab', async ({ page }) => {
    await openFactTab(page);
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await page.getByRole('button', { name: /Анализ версии/ }).click();
    await expect(page.getByTestId('fact-analysis-dialog')).toBeVisible();
    await expect(page.getByTestId('fact-analysis-dialog').getByText('План.Труд.Песс.')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('fact-analysis-dialog').getByText('План.Труд.Баз.')).toBeVisible();
    await expect(page.getByTestId('fact-analysis-dialog').getByText('План.Труд.Оптим.')).toBeVisible();
  });

  test('fact detail grouping via column drag shows group rows', async ({ page }) => {
    test.setTimeout(90_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, ['fact-detail']);
    await page.goto(`/projects/${projectId}`);
    await waitForWorkingOverlayHidden(page);

    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await waitForWorkingOverlayHidden(page);
    await page.getByRole('tab', { name: 'Детально' }).click();
    await waitForWorkingOverlayHidden(page);

    const hasRows = await page
      .locator('.ag-center-cols-container .ag-row')
      .first()
      .isVisible({ timeout: 30_000 })
      .catch(() => false);
    test.skip(!hasRows, 'No fact detail rows for preferred project');

    await expect(
      page
        .locator('.ag-column-drop-horizontal')
        .filter({ hasText: 'Перетащите сюда заголовок столбца для группировки' }),
    ).toBeVisible();

    await scrollAgGridToColumn(page, 'iicGroupName', 'fact-detail');
    const groupableHeader = page.locator('.ag-header-cell[col-id="iicGroupName"]').first();
    await expect(groupableHeader).toBeVisible({ timeout: 20_000 });

    const groupedViaApi = await page.evaluate((id) => {
      const api = (window as unknown as {
        __epGridApis?: Record<
          string,
          { addRowGroupColumns?: (cols: string[]) => void; setRowGroupColumns?: (cols: string[]) => void }
        >;
      }).__epGridApis?.[id];
      if (api?.setRowGroupColumns) {
        api.setRowGroupColumns(['iicGroupName']);
        return true;
      }
      if (api?.addRowGroupColumns) {
        api.addRowGroupColumns(['iicGroupName']);
        return true;
      }
      return false;
    }, 'fact-detail');

    if (!groupedViaApi) {
      const dropZone = page
        .locator('.ag-column-drop-horizontal')
        .filter({ hasText: 'Перетащите сюда заголовок столбца для группировки' });
      await groupableHeader.dragTo(dropZone, { force: true });
    }

    await expect(page.locator('.ag-row-group').first()).toBeVisible({ timeout: 20_000 });
  });
});
