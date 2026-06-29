import { test, expect } from '@playwright/test';
import { logoutViaUserMenu } from './helpers';

test.describe('Login page identity', () => {
  test('does not show simulated dev Windows username', async ({ page }) => {
    await page.route('**/api/auth/negotiate', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }),
    );
    await page.route('**/api/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }),
    );

    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForSelector('text=Авторизация', { timeout: 20_000 });

    await expect(page.getByText('Введите учетные данные')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/LAPTOP-ANTON/i)).toHaveCount(0);
    await expect(page.getByText(/Windows-пользователь/i)).toHaveCount(0);
  });

  test('shows welcome name from proxy identity hint', async ({ page }) => {
    await page.route('**/api/auth/negotiate', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }),
    );
    await page.route('**/api/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }),
    );
    await page.route('**/api/auth/windows-identity', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          windowsUsername: 'CORP\\ivanov',
          displayName: 'Иванов И.И.',
          source: 'proxy',
        }),
      }),
    );

    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForSelector('text=Авторизация', { timeout: 20_000 });

    await expect(page.getByText('Добро пожаловать, Иванов И.И.')).toBeVisible();
  });

  test('after logout login page has no dev windows block', async ({ page }) => {
    await page.goto('/');
    const auth = page.getByText('Авторизация');
    if (!(await auth.isVisible().catch(() => false))) {
      await logoutViaUserMenu(page);
    }
    await expect(page.getByText('Авторизация')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/LAPTOP-ANTON/i)).toHaveCount(0);
    await expect(page.getByText(/Windows-пользователь/i)).toHaveCount(0);
  });
});
