import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('DocSet directory hidden', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('вкладка комплектов документации не отображается', async ({ page }) => {
    await page.goto('/references/labor-intensities');
    await expect(page).toHaveURL(/\/references\/labor-intensities/, { timeout: 20_000 });
    await expect(page.getByRole('link', { name: 'Комплекты документации' })).toHaveCount(0);
  });
});
