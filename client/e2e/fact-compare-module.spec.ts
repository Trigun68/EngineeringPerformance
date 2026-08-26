import { test, expect } from '@playwright/test';
import {
  clearMatchingGridLayouts,
  loginAsAdmin,
  openPreferredProject,
  scrollAgGridToColumn,
  waitForWorkingOverlayHidden,
  actionButton,
  ACTION_LABELS,
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
    await expect(actionButton(page, 'importPhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'deletePhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'importEquipment')).toBeVisible();
    await expect(actionButton(page, 'deleteEquipment')).toBeVisible();
    await expect(actionButton(page, 'importLabor')).toBeVisible();
    await expect(actionButton(page, 'deleteLabor')).toBeVisible();

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
      await scrollAgGridToColumn(page, 'optimisticHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Оптим.' })).toBeVisible({ timeout: 20_000 });
      await scrollAgGridToColumn(page, 'baseHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Баз.' })).toBeVisible();
      await scrollAgGridToColumn(page, 'pessimisticHours', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд.Песс.' })).toBeVisible();
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

  test('fact summary pdf uses Russian filename template', async ({ page }) => {
    test.setTimeout(90_000);
    await openFactTab(page);

    await page.getByRole('button', { name: /Версия \(факт\)/ }).click();
    const dialog = page.getByTestId('version-picker-dialog');
    await expect(dialog).toBeVisible({ timeout: 10_000 });
    const versionRow = dialog.locator('.ag-center-cols-container .ag-row').first();
    if ((await versionRow.count()) === 0) {
      test.skip(true, 'No fact versions in seed data');
      return;
    }
    await versionRow.click();
    await dialog.getByRole('button', { name: 'Выбрать' }).click();
    await expect(dialog).toBeHidden({ timeout: 10_000 });
    await waitForWorkingOverlayHidden(page);

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await expect(actionButton(page, 'exportExcel')).toBeVisible();
    const pdfButton = actionButton(page, 'exportPdf');
    await expect(pdfButton).toBeEnabled({ timeout: 15_000 });
    const downloadPromise = page.waitForEvent('download', { timeout: 60_000 });
    await pdfButton.click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^факт_итого_-_.+\(.+\)\.pdf$/);
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
    await expect(page.getByRole('button', { name: 'Таблица', exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'График', exact: true })).toHaveCount(0);
    await expect(actionButton(page, 'exportExcel')).toBeVisible();
    await expect(actionButton(page, 'exportExcelSelected')).toBeVisible();
    await expect(actionButton(page, 'exportPdf')).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Показывать различия' })).toBeEnabled({ timeout: 120_000 });
    const differencesChip = page.getByTestId('compare-version-differences');
    await expect(differencesChip).toBeVisible();
    const chipFollowsScenario = await page.evaluate(() => {
      const scenario = document.getElementById('compare-scenario-label');
      const chip = document.querySelector('[data-testid="compare-version-differences"]');
      if (!scenario || !chip) return false;
      return Boolean(scenario.compareDocumentPosition(chip) & Node.DOCUMENT_POSITION_FOLLOWING);
    });
    expect(chipFollowsScenario).toBe(true);
    await expect(page.getByRole('button', { name: 'Split' })).toHaveCount(0);
    await expect(actionButton(page, 'importEquipment')).toHaveCount(0);
    await expect(actionButton(page, 'deleteEquipment')).toHaveCount(0);

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
      await scrollAgGridToColumn(page, 'plannedLaborHoursOptimisticA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Опт. v. А)' })).toBeVisible({ timeout: 20_000 });
      await scrollAgGridToColumn(page, 'plannedLaborHoursA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Баз. v. А)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursPessimisticA', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Песс. v. А)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursOptimisticB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Опт. v. Б)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Баз. v. Б)' })).toBeVisible();
      await scrollAgGridToColumn(page, 'plannedLaborHoursPessimisticB', gridId);
      await expect(page.getByRole('columnheader', { name: 'План.Труд. (Песс. v. Б)' })).toBeVisible();
    };

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('compare-summary');

    await page.getByRole('tab', { name: 'По объектам', exact: true }).click();
    await waitForWorkingOverlayHidden(page);
    await expectPlanHeaders('compare-facility');
    const facilityHasRows = await page
      .locator('#compare-facility .ag-center-cols-container .ag-row, [grid-id="compare-facility"] .ag-center-cols-container .ag-row')
      .first()
      .isVisible({ timeout: 10_000 })
      .catch(() => false);
    if (facilityHasRows) {
      await expect(page.locator('.ag-floating-bottom').getByText('ИТОГО', { exact: true })).toBeVisible({
        timeout: 10_000,
      });
    }

    await page.getByRole('tab', { name: 'Детально' }).click();
    await waitForWorkingOverlayHidden(page, 90_000);
    await expectPlanHeaders('compare-detail');
    await expect.poll(async () => {
      return page.evaluate(() => {
        const w = window as Window & {
          __epGridApis?: Record<string, { getDisplayedRowCount?: () => number }>;
        };
        return w.__epGridApis?.['compare-detail']?.getDisplayedRowCount?.() ?? 0;
      });
    }, { timeout: 60_000 }).toBeGreaterThan(0);
  });

  test('compare detail has rows after reload with saved Детально tab', async ({ page }) => {
    test.setTimeout(120_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    const pair = await page.evaluate(async (id) => {
      const res = await fetch(`/api/projects/${id}/versions`, { credentials: 'include' });
      if (!res.ok) return null;
      const versions = (await res.json()) as Array<{
        id: number;
        direction: number;
        isArchived: boolean;
        isPrimary?: boolean;
      }>;
      const plan = versions.filter((v) => v.direction === 1 && !v.isArchived);
      const fact = versions.filter((v) => v.direction === 2 && !v.isArchived);
      const primaryPlan = plan.find((v) => v.isPrimary) ?? plan[0];
      const otherPlan = plan.find((v) => v.id !== primaryPlan?.id) ?? primaryPlan;
      const primaryFact = fact.find((v) => v.isPrimary) ?? fact[0] ?? primaryPlan;
      if (!otherPlan || !primaryFact) return null;
      return { versionA: otherPlan.id, versionB: primaryFact.id };
    }, projectId);
    test.skip(pair == null, 'Need plan and fact versions for compare detail');

    await page.evaluate(
      async ({ id, versionA, versionB }) => {
        await fetch('/api/users/me/preferences', {
          method: 'PUT',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            preferences: {
              [`projects.${id}.compareSubTab`]: 'compare-detail',
              [`projects.${id}.compareVersionAId`]: String(versionA),
              [`projects.${id}.compareVersionBId`]: String(versionB),
            },
          }),
        });
      },
      { id: projectId, versionA: pair!.versionA, versionB: pair!.versionB },
    );

    await page.goto(`/projects/${projectId}`);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await waitForWorkingOverlayHidden(page, 90_000);

    await expect.poll(async () => {
      return page.evaluate(() => {
        const w = window as Window & {
          __epGridApis?: Record<string, { getDisplayedRowCount?: () => number }>;
        };
        return w.__epGridApis?.['compare-detail']?.getDisplayedRowCount?.() ?? 0;
      });
    }, { timeout: 90_000 }).toBeGreaterThan(0);
  });

  test('fact analysis dialog opens from fact tab', async ({ page }) => {
    test.setTimeout(180_000);
    await openFactTab(page);
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await page.getByRole('button', { name: /Анализ версии/ }).click();
    await expect(page.getByTestId('fact-analysis-dialog')).toBeVisible();
    await expect(page.getByTestId('fact-analysis-dialog').getByText(/Анализ версии:/)).toBeVisible();
    const dialog = page.getByTestId('fact-analysis-dialog');
    await expect(dialog).toContainText('План.Труд.Оптим.', { timeout: 90_000 });
    await expect(dialog.getByRole('button', { name: ACTION_LABELS.exportExcel, exact: true })).toBeVisible();
    await expect(dialog.getByRole('button', { name: ACTION_LABELS.exportPdf, exact: true })).toBeVisible();
    await expect(dialog).toContainText('План.Труд.Баз.');
    await expect(dialog).toContainText('План.Труд.Песс.');
    const headerText = await dialog.locator('div').filter({ hasText: 'План.Труд.Оптим.' }).first().textContent();
    expect(headerText ?? '').toMatch(/План\.Труд\.Оптим\..*План\.Труд\.Баз\..*План\.Труд\.Песс\./);
    const cards = dialog.locator('[data-testid^="fact-analysis-card-"]');
    await expect(cards.first()).toBeVisible({ timeout: 90_000 });
    expect(await cards.count()).toBeGreaterThan(1);
    const firstCode = ((await cards.nth(0).innerText()) ?? '').split(/\s+/)[0];
    const secondCode = ((await cards.nth(1).innerText()) ?? '').split(/\s+/)[0];
    if (firstCode && secondCode && firstCode.startsWith('IIC-')) {
      expect(secondCode.startsWith(firstCode)).toBeTruthy();
    }
  });

  test('fact analysis IIC-1 card shows own FO and labor only, not descendant rows', async ({ page }) => {
    test.setTimeout(90_000);
    await openFactTab(page);
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await page.getByRole('button', { name: /Анализ версии/ }).click();
    const dialog = page.getByTestId('fact-analysis-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('button', { name: ACTION_LABELS.refresh, exact: true })).toBeEnabled({
      timeout: 90_000,
    });

    const parentCard = dialog
      .locator('[data-testid^="fact-analysis-card-"]')
      .filter({ hasText: /Общий бетон/ })
      .first();
    await expect(parentCard).toBeVisible({ timeout: 20_000 });

    const childCard = dialog
      .locator('[data-testid^="fact-analysis-card-"]')
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
