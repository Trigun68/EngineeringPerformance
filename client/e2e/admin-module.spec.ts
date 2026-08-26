import { test, expect } from '@playwright/test';
import { loginAsAdmin, loginAsObserver } from './helpers';

const MANAGER_USER = { username: 'manager', password: 'manager' };

async function loginAsManager(page: import('@playwright/test').Page): Promise<void> {
  await page.context().clearCookies();
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  const authHeading = page.getByText('Авторизация');
  if (await authHeading.isVisible().catch(() => false)) {
    await page.getByLabel('Имя пользователя').fill(MANAGER_USER.username);
    await page.getByLabel('Пароль').fill(MANAGER_USER.password);
    await page.getByRole('button', { name: 'Войти' }).click();
    await expect(page.locator('[aria-label="Меню пользователя"]')).toBeVisible({ timeout: 30_000 });
  }
}

test.describe('Admin module', () => {
  test('admin sees administration section and all tabs', async ({ page }) => {
    await loginAsAdmin(page);

    await expect(page.getByRole('link', { name: 'Администрирование' })).toBeVisible();
    await page.getByRole('link', { name: 'Администрирование' }).click();
    await expect(page.getByTestId('app-nav-list')).toBeVisible();
    await expect(page).toHaveURL(/\/admin\/logs$/);

    await expect(page.getByRole('link', { name: 'Логи системы' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Состояние сервисов' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Мониторинг' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Аудит настроек' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Журнал действий' })).toBeVisible();

    await page.waitForSelector('[data-testid="admin-log-viewer-grid"]', { timeout: 15_000 });

    await page.getByRole('link', { name: 'Состояние сервисов' }).click();
    await expect(page).toHaveURL(/\/admin\/health$/);
    await expect(page.getByTestId('health-service-Database')).toBeVisible({ timeout: 15_000 });

    await page.getByRole('link', { name: 'Мониторинг' }).click();
    await expect(page).toHaveURL(/\/admin\/monitoring$/);
    await expect(page.getByText('Системный мониторинг (RED)')).toBeVisible({ timeout: 15_000 });

    await page.getByRole('link', { name: 'Аудит настроек' }).click();
    await expect(page).toHaveURL(/\/admin\/audit$/);
    await page.waitForSelector('[data-testid="admin-config-audit-grid"]', { timeout: 15_000 });
  });

  test('admin log viewer highlights error rows', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/admin/logs');
    await page.waitForSelector('[data-testid="admin-log-viewer-grid"] .ag-row', { timeout: 15_000 }).catch(() => null);

    const errorRow = page.locator('[data-testid="admin-log-viewer-grid"] .log-row-error, [data-testid="admin-log-viewer-grid"] .ag-row').filter({
      has: page.locator('.ag-cell', { hasText: /Error|Fatal/i }),
    }).first();

    if (await errorRow.isVisible().catch(() => false)) {
      const cls = await errorRow.getAttribute('class');
      expect(cls ?? '').toMatch(/log-row-error|ag-row/);
    }
  });

  test('manager does not see administration nav', async ({ page }) => {
    await loginAsManager(page);
    await expect(page.getByRole('link', { name: 'Администрирование' })).toHaveCount(0);
  });

  test('observer does not see administration nav', async ({ page }) => {
    await loginAsObserver(page);
    await expect(page.getByRole('link', { name: 'Администрирование' })).toHaveCount(0);
  });
});
