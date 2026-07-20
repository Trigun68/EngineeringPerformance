import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test('BER grid shows linked KFO column', async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto('/references/physical-volumes-ber');
  await page.waitForSelector('.ag-root', { timeout: 20_000 });
  await expect(page.getByRole('columnheader', { name: 'Связанная норма КФО' })).toBeVisible({ timeout: 15_000 });
  await page.getByRole('button', { name: 'Развернуть всё' }).click();
  await page.waitForTimeout(1000);
  // open edit on a leaf with data
  const cell = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: 'KQ.' }).first();
  if (await cell.count()) {
    await cell.dblclick();
    await expect(page.getByText('Связанная номенклатура КФО')).toBeVisible({ timeout: 10_000 });
  }
});
