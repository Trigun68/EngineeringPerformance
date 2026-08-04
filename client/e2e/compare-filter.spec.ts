import { test, expect, type Page } from '@playwright/test';
import {
  clearMatchingGridLayouts,
  loginAsAdmin,
  openPreferredProject,
  waitForWorkingOverlayHidden,
} from './helpers';

/** Число строк модели AG Grid (не DOM — виртуализация даёт ~viewport). */
async function getGridDisplayedRowCount(page: Page, gridId: string): Promise<number> {
  return page.evaluate((id) => {
    const api = (window as Window & {
      __epGridApis?: Record<string, { getDisplayedRowCount: () => number }>;
    }).__epGridApis?.[id];
    return api?.getDisplayedRowCount() ?? -1;
  }, gridId);
}

async function openCompareTab(page: Page, projectId: number) {
  await page.goto(`/projects/${projectId}`);
  await waitForWorkingOverlayHidden(page);
  await page.getByRole('tab', { name: 'Сравнение версий' }).click();
  await waitForWorkingOverlayHidden(page);

  const scenario = page.getByLabel('Сценарий');
  const hasCompareUi = await scenario.isVisible({ timeout: 20_000 }).catch(() => false);
  test.skip(!hasCompareUi, 'No compare UI for preferred project');

  // WorkingOverlay на compare имеет delayMs=1000 — ждём данные по enabled чекбоксу.
  const differences = page.getByRole('checkbox', { name: 'Показывать различия' });
  await expect(differences).toBeEnabled({ timeout: 120_000 });
}

async function enableShowOnlyDifferences(page: Page) {
  const checkbox = page.getByRole('checkbox', { name: 'Показывать различия' });
  // Controlled MUI: click(), не check() — иначе DOM checked без React state.
  if (!(await checkbox.isChecked())) {
    await checkbox.click();
  }
  await expect(checkbox).toBeChecked();
}

test.describe('Compare filter behavior', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('show only differences hides identical facility rows on plan vs fact', async ({ page }) => {
    test.setTimeout(180_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, [
      'compare-summary',
      'compare-facility',
      'compare-detail',
    ]);

    await openCompareTab(page, projectId);

    await page.getByRole('tab', { name: 'По объектам', exact: true }).click();
    await waitForWorkingOverlayHidden(page);

    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-facility'), { timeout: 60_000 })
      .toBeGreaterThan(0);

    const rowsBefore = await getGridDisplayedRowCount(page, 'compare-facility');
    await enableShowOnlyDifferences(page);

    // Дефолт план↔факт: оба factLaborHours null → все объекты идентичны.
    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-facility'), { timeout: 15_000 })
      .toBe(0);
    expect(rowsBefore).toBeGreaterThan(0);
  });

  test('show only differences reduces summary row count', async ({ page }) => {
    test.setTimeout(180_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await openCompareTab(page, projectId);

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page);

    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-summary'), { timeout: 60_000 })
      .toBeGreaterThan(0);

    const rowsBefore = await getGridDisplayedRowCount(page, 'compare-summary');
    await enableShowOnlyDifferences(page);

    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-summary'), { timeout: 15_000 })
      .toBeLessThan(rowsBefore);
  });
});
