import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Planning scenarios', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('projects page loads forecast workspace', async ({ page }) => {
    await page.goto('/projects');
    await expect(page.locator('.ag-root-wrapper').first()).toBeVisible({ timeout: 20_000 });
  });
});
