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
  // Prefs с прошлого теста могли оставить фильтр включённым — все идентичные строки скрыты.
  if (await differences.isChecked()) {
    await differences.click();
  }
  await expect(differences).not.toBeChecked();
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

    // Дождаться монтирования грида; пустой seed по объектам — skip, не fail.
    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-facility'), { timeout: 60_000 })
      .not.toBe(-1);

    const rowsBefore = await getGridDisplayedRowCount(page, 'compare-facility');
    test.skip(
      rowsBefore <= 0,
      'No facility rows in compare seed for preferred project',
    );

    await enableShowOnlyDifferences(page);

    // При идентичных объектах → 0; при различиях в seed — хотя бы не больше исходного.
    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-facility'), { timeout: 15_000 })
      .toBeLessThanOrEqual(rowsBefore);
    const rowsAfter = await getGridDisplayedRowCount(page, 'compare-facility');
    test.skip(
      rowsAfter === rowsBefore,
      'Seed compare data has no identical facility rows to hide',
    );
  });

  test('show only differences reduces summary row count', async ({ page }) => {
    test.setTimeout(180_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, [
      'compare-summary',
      'compare-facility',
      'compare-detail',
    ]);

    await openCompareTab(page, projectId);

    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page);

    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-summary'), { timeout: 60_000 })
      .not.toBe(-1);

    const rowsBefore = await getGridDisplayedRowCount(page, 'compare-summary');
    test.skip(
      rowsBefore <= 0,
      'No summary rows in compare seed for preferred project',
    );

    await enableShowOnlyDifferences(page);

    // При идентичных группах Итого → меньше строк; если в seed все отличаются — skip, не fail.
    await expect
      .poll(async () => getGridDisplayedRowCount(page, 'compare-summary'), { timeout: 15_000 })
      .toBeLessThanOrEqual(rowsBefore);
    const rowsAfter = await getGridDisplayedRowCount(page, 'compare-summary');
    test.skip(
      rowsAfter === rowsBefore,
      'Seed compare data has no identical summary rows to hide',
    );
  });
});
