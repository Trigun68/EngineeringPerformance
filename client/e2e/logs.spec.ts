import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Audit logs page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('shows toolbar actions and grid', async ({ page }) => {
    await page.goto('/admin/user-actions');
    await expect(page.getByRole('heading', { name: 'Журнал действий пользователей' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Выгрузить в Excel' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'В архив' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Восстановить' })).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });

  test('toggle show archived checkbox', async ({ page }) => {
    await page.goto('/admin/user-actions');
    const checkbox = page.getByRole('checkbox', { name: 'Показать архивные' });
    await expect(checkbox).toBeVisible();
    await checkbox.check();
    await expect(checkbox).toBeChecked();
  });
});
