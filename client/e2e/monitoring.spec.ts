import { test, expect } from '@playwright/test';
import { loginAsAdmin, loginAsObserver } from './helpers';

test.describe('Monitoring navigation', () => {
  const MANAGER_USER = { username: 'manager', password: 'manager' };

  test('admin opens Monitoring via Administration tab', async ({ page }) => {
    await loginAsAdmin(page);
    await expect(page.getByTestId('app-nav-list').getByRole('link', { name: 'Мониторинг' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Администрирование' })).toBeVisible();
    await page.getByRole('link', { name: 'Администрирование' }).click();
    await page.getByRole('link', { name: 'Мониторинг' }).click();
    await expect(page).toHaveURL(/\/admin\/monitoring$/);
    await expect(page.getByText('Системный мониторинг (RED)')).toBeVisible({ timeout: 15_000 });
  });

  test('legacy /monitoring redirects to /admin/monitoring', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/monitoring');
    await expect(page).toHaveURL(/\/admin\/monitoring$/);
    await expect(page.getByText('Системный мониторинг (RED)')).toBeVisible({ timeout: 15_000 });
  });

  test('manager does not see Administration nav', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    const authHeading = page.getByText('Авторизация');
    if (await authHeading.isVisible().catch(() => false)) {
      await page.getByLabel('Имя пользователя').fill(MANAGER_USER.username);
      await page.getByLabel('Пароль').fill(MANAGER_USER.password);
      await page.getByRole('button', { name: 'Войти' }).click();
    }
    await expect(page.getByRole('link', { name: 'Администрирование' })).toHaveCount(0);
  });

  test('observer does not see Administration nav', async ({ page }) => {
    await loginAsObserver(page);
    await expect(page.getByRole('link', { name: 'Администрирование' })).toHaveCount(0);
  });
});
