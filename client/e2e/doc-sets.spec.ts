import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('DocSet directory', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('opens комплекты документации', async ({ page }) => {
    await page.goto('/references/doc-sets');
    await expect(page).toHaveURL(/\/references\/doc-sets/);
    // Вкладки справочников — NavLink (role=link), не button.
    const tab = page.getByRole('link', { name: 'Комплекты документации' });
    await tab.scrollIntoViewIfNeeded();
    await expect(tab).toBeVisible({ timeout: 20_000 });
    await page.waitForSelector('.ag-root', { timeout: 20_000 });
  });
});
