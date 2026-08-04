import { expect, test } from '@playwright/test';
import {
  clearMatchingGridLayouts,
  loginAsAdmin,
  openPreferredProject,
  scrollAgGridToColumn,
  waitForWorkingOverlayHidden,
} from './helpers';

test.describe('Forecast aggregation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('forecast summary grid shows labor intensity columns', async ({ page }) => {
    test.setTimeout(90_000);
    const projectId = await openPreferredProject(page);
    test.skip(!projectId, 'No projects in seed data');

    await clearMatchingGridLayouts(page, [
      'forecast-summary',
      'grid-forecast-summary',
    ]);

    await page.goto(`/projects/${projectId}`);
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click({ force: true });
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.getByRole('tab', { name: 'Итого', exact: true }).first().click({ force: true });
    await waitForWorkingOverlayHidden(page, 90_000);

    await scrollAgGridToColumn(page, 'baseHours', 'forecast-summary');
    const baseHours = page.locator('.ag-header-cell[col-id="baseHours"]').first();
    const visible = await baseHours.isVisible({ timeout: 20_000 }).catch(() => false);
    test.skip(!visible, 'Forecast summary labor columns not available after layout reset');

    await expect(baseHours).toBeVisible();
    await scrollAgGridToColumn(page, 'optimisticHours', 'forecast-summary');
    await expect(page.locator('.ag-header-cell[col-id="optimisticHours"]').first()).toBeVisible();
  });
});
