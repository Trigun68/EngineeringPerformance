import { test, expect } from '@playwright/test';
import { loginAsAdmin, loginAsObserver, logoutViaUserMenu, waitForWorkingOverlayHidden } from './helpers';

test.describe('Permissions matrix', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('loads grid with headers and hierarchy', async ({ page }) => {
    await page.goto('/settings/access-rights');
    await expect(page.getByText('Матрица прав доступа')).toBeVisible({ timeout: 20_000 });
    await page.waitForSelector('[data-testid="permission-matrix-grid"] .ag-root', { timeout: 15_000 });
    const grid = page.getByTestId('permission-matrix-grid');
    await expect(grid.locator('.ag-header-cell-text', { hasText: 'Объект / Роль' })).toBeVisible();
    // Exact: иначе «Суперадминистратор» тоже матчится как substring.
    await expect(grid.locator('.ag-header-cell-text', { hasText: /^Администратор$/ })).toBeVisible();
    await expect(page.getByTestId('object-cell-Assessment')).toBeVisible();
  });

  test('collapses Projects group and hides children', async ({ page }) => {
    await page.goto('/settings/access-rights');
    await waitForWorkingOverlayHidden(page);
    await page.waitForSelector('[data-testid="permission-matrix-grid"] .ag-root', { timeout: 15_000 });
    const toggle = page.getByTestId('toggle-section-ProjectsGroup');
    await expect(toggle).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('object-cell-Assessment')).toBeVisible();
    await toggle.click();
    await expect(page.getByTestId('object-cell-Assessment')).toHaveCount(0);
    await expect(page.getByTestId('object-cell-ProjectsGroup')).toBeVisible();
  });

  test('shows Settings and UserActionLog in matrix', async ({ page }) => {
    await page.goto('/settings/access-rights');
    await page.waitForSelector('[data-testid="permission-matrix-grid"] .ag-root', { timeout: 15_000 });

    await expect(
      page.getByTestId('app-nav-list').getByRole('link', { name: 'Настройки', exact: true }),
    ).toHaveClass(/active/);

    // Нижние строки виртуализированы — прокручиваем viewport грида.
    await page.locator('[data-testid="permission-matrix-grid"] .ag-body-viewport').evaluate((el) => {
      el.scrollTop = el.scrollHeight;
    });

    await expect(page.getByTestId('object-cell-SettingsPage')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('object-cell-Users')).toBeVisible();
    await expect(page.getByTestId('object-cell-UserActionLog')).toBeVisible();
  });

  test('none access hides object from observer navigation after re-login', async ({ page }) => {
    await page.goto('/settings/access-rights');
    await page.waitForSelector('[data-testid="permission-matrix-grid"] .ag-root', { timeout: 15_000 });

    const matrixState = await page.evaluate(async () => {
      const response = await fetch('/api/permissions/matrix', { credentials: 'include' });
      const data = await response.json() as {
        roles?: Array<{ id: number; name: string }>;
        Roles?: Array<{ id: number; name: string }>;
        matrix?: Record<string, Record<string, string>>;
        Matrix?: Record<string, Record<string, string>>;
      };
      const roles = data.roles ?? data.Roles ?? [];
      const observer = roles.find((role) => role.name === 'Engineer');
      const matrix = data.matrix ?? data.Matrix ?? {};
      const observerId = observer?.id ?? null;
      return {
        observerRoleId: observerId,
        dashboardLevel: observerId
          ? matrix.Dashboard?.[String(observerId)] ?? 'None'
          : 'None',
      };
    });
    expect(matrixState.observerRoleId).not.toBeNull();

    const dashboardSelect = page.getByTestId(`role-select-Dashboard-${matrixState.observerRoleId}`);
    await dashboardSelect.waitFor({ state: 'visible', timeout: 15_000 });

    if (matrixState.dashboardLevel !== 'None') {
      const saveResponse = page.waitForResponse(
        (response) =>
          response.url().includes('/permissions/matrix') && response.request().method() === 'PUT',
      );
      await dashboardSelect.click();
      await page.getByRole('option', { name: 'Нет доступа' }).click();
      await saveResponse;
    }

    await logoutViaUserMenu(page);
    await loginAsObserver(page);

    await expect(page.getByRole('link', { name: 'Главная' })).toHaveCount(0);
  });
});
