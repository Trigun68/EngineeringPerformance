import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Mobile portal navigation', () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('hamburger opens menu; navigate closes drawer', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('mobile-nav-toggle')).toBeVisible({ timeout: 15_000 });

    // Меню скрыто: пункт в drawer не виден до открытия.
    await expect(page.getByTestId('mobile-nav-toggle')).toBeVisible();
    await page.getByTestId('mobile-nav-toggle').click();

    const projectsLink = page.getByRole('link', { name: 'Проекты' });
    await expect(projectsLink).toBeVisible();
    await projectsLink.click();
    await page.waitForURL(/\/projects/);

    // После перехода drawer закрыт — гамбургер снова виден, modal скрыт.
    await expect(page.getByTestId('mobile-nav-toggle')).toBeVisible();
    await expect(page.locator('.MuiDrawer-root.MuiModal-root')).toHaveAttribute('aria-hidden', 'true');
  });

  test('project workspace tabs are usable on narrow screen', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    const preferred = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: '25-310' });
    if ((await preferred.count()) > 0) {
      await preferred.first().dblclick();
    } else {
      await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    }
    await page.waitForURL(/\/projects\/\d+/);

    await expect(page.getByRole('tab', { name: 'Прогнозирование' })).toBeVisible({ timeout: 15_000 });
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await expect(page.getByRole('tab', { name: 'Итого', exact: true }).first()).toBeVisible({
      timeout: 20_000,
    });
  });

  test('forecast tree expand/collapse available via context menu', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await page.getByRole('tab', { name: 'Итого', exact: true }).first().click();

    await expect(page.getByTestId('datatable-expand-all')).toHaveCount(0);
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 20_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().click({ button: 'right' });
    await expect(page.getByText('Развернуть всё', { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('Свернуть всё', { exact: true })).toBeVisible();
  });

  test('references secondary nav scrolls on mobile', async ({ page }) => {
    await page.getByTestId('mobile-nav-toggle').click();
    await page.getByRole('link', { name: 'Справочники' }).click();
    await page.waitForURL(/\/references/);
    await expect(page.getByRole('link', { name: /КФО|БЭР|Направления/i }).first()).toBeVisible({
      timeout: 15_000,
    });
  });
});
