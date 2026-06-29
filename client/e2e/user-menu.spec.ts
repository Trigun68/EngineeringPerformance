import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('User menu', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('opens profile menu and change password dialog', async ({ page }) => {
    await page.locator('[aria-label="Меню пользователя"]').click();
    await expect(page.getByRole('menuitem', { name: 'Сменить пароль' })).toBeVisible();
    await page.getByRole('menuitem', { name: 'Сменить пароль' }).click();
    await expect(page.getByText('Смена пароля')).toBeVisible();
    await expect(page.getByLabel('Текущий пароль')).toBeVisible();
  });
});
