import { expect, type Page } from '@playwright/test';
import { test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

/** Растягивает колонку AG Grid за ручку resize и возвращает ширины до/после. */
async function resizeColumn(
  page: Page,
  target: { headerText?: string | RegExp; colId?: string },
  deltaPx: number,
): Promise<{ before: number; after: number }> {
  const header = target.colId
    ? page.locator(`.ag-header-cell[col-id="${target.colId}"]`).first()
    : page
        .locator('.ag-header-cell:not(.ag-header-group-cell)')
        .filter({ hasText: target.headerText! })
        .first();
  await expect(header).toBeVisible({ timeout: 20_000 });
  await header.evaluate((el) => el.scrollIntoView({ inline: 'center', block: 'nearest' }));

  const beforeBox = await header.boundingBox();
  expect(beforeBox).toBeTruthy();
  const before = beforeBox!.width;

  const handle = header.locator('.ag-header-cell-resize');
  await expect(handle).toBeVisible();
  const handleBox = await handle.boundingBox();
  expect(handleBox).toBeTruthy();

  const x = handleBox!.x + handleBox!.width / 2;
  const y = handleBox!.y + handleBox!.height / 2;
  await page.mouse.move(x, y);
  await page.mouse.down();
  await page.mouse.move(x + deltaPx, y, { steps: 12 });
  await page.mouse.up();
  await expect
    .poll(async () => {
      const box = await header.boundingBox().catch(() => null);
      return box?.width ?? 0;
    }, { timeout: 5_000 })
    .toBeGreaterThan(before + Math.min(20, Math.abs(deltaPx) / 2));

  const afterBox = await header.boundingBox();
  expect(afterBox).toBeTruthy();
  return { before, after: afterBox!.width };
}

async function openFirstProject(page: Page): Promise<void> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  // Предпочитаем проект с версиями/данными (25-310), иначе первую строку.
  const preferred = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: '25-310' });
  if ((await preferred.count()) > 0) {
    await preferred.first().dblclick();
  } else {
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  }
  await page.waitForURL(/\/projects\/\d+/);
}

async function expectColumnWidens(
  page: Page,
  target: { headerText?: string | RegExp; colId?: string },
  deltaPx = 100,
): Promise<void> {
  const label = target.colId ?? String(target.headerText);
  const { before, after } = await resizeColumn(page, target, deltaPx);
  expect(after, `column "${label}" should widen`).toBeGreaterThan(before + 30);
}

test.describe('Project grids — column resize', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openFirstProject(page);
  });

  test('Forecast Итого: column width changes on drag', async ({ page }) => {
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('tab', { name: 'Итого', exact: true }).first().click();
    await expect(page.locator('.ag-header-cell').filter({ hasText: 'Базовый' }).first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'baseHours' });
  });

  test('Forecast Итого по объектам: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('tab', { name: 'Итого по объектам' }).click();
    await expect(page.locator('.ag-header-cell').filter({ hasText: 'Базовый' }).first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'baseHours' });
  });

  test('Forecast Детально: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.locator('.ag-header-cell').filter({ hasText: 'Шифр объекта' }).first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'facilityCode' });
  });

  test('Fact Итого: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await expect(
      page.locator('.ag-header-cell').filter({ hasText: /Факт трудоёмк/ }).first(),
    ).toBeVisible({ timeout: 20_000 });
    await expectColumnWidens(page, { colId: 'factLaborHours' });
  });

  test('Fact Итого по объектам: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await page.getByRole('tab', { name: 'Итого по объектам' }).click();
    await expect(page.locator('.ag-header-cell').filter({ hasText: 'Z-score' }).first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'zScore' });
  });

  test('Fact Детально: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.locator('.ag-header-cell').filter({ hasText: 'Шифр объекта' }).first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'facilityCode' });
  });

  test('Compare Итого: column width changes when grid visible', async ({ page }) => {
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await expect(page.locator('.ag-header-cell[col-id="zDelta"]').first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'zDelta' });
  });

  test('Compare Итого по объектам: column width changes', async ({ page }) => {
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();
    await page.getByRole('tab', { name: 'Итого по объектам' }).click();
    await expect(page.locator('.ag-header-cell[col-id="hoursDelta"]').first()).toBeVisible({
      timeout: 20_000,
    });
    await expectColumnWidens(page, { colId: 'hoursDelta' });
  });

  test('Forecast Итого: width persists across sub-tab switch', async ({ page }) => {
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('tab', { name: 'Итого', exact: true }).first().click();
    // Узкая левая колонка — ручка resize не уезжает за край viewport.
    const header = page.locator('.ag-header-cell[col-id="factPhysicalVolume"]').first();
    await expect(header).toBeVisible({ timeout: 20_000 });

    // Сначала сужаем (на случай сохранённой широкой ширины), затем задаём отличимую ширину.
    await resizeColumn(page, { colId: 'factPhysicalVolume' }, -80).catch(() => undefined);
    const prefsSave = page.waitForResponse(
      (r) => r.url().includes('/preferences') && r.request().method() === 'PUT',
      { timeout: 15_000 },
    );
    const { after: widened } = await resizeColumn(page, { colId: 'factPhysicalVolume' }, 90);
    expect(widened).toBeGreaterThan(140);
    await prefsSave.catch(() => undefined);
    await page.waitForTimeout(500);

    await page.getByRole('tab', { name: 'Детально' }).click();
    await expect(page.locator('.ag-header-cell[col-id="facilityCode"]').first()).toBeVisible({
      timeout: 15_000,
    });
    await page.getByRole('tab', { name: 'Итого', exact: true }).first().click();
    await expect(header).toBeVisible({ timeout: 20_000 });
    await expect
      .poll(async () => (await header.boundingBox())?.width ?? 0, { timeout: 10_000 })
      .toBeGreaterThan(widened - 25);
  });
});
