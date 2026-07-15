import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Parametric norms', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('IIC norms page loads and shows reference grid', async ({ page }) => {
    await page.goto('/references/iic-norms');
    await expect(page.locator('.ag-root-wrapper')).toBeVisible({ timeout: 15_000 });
  });
});
