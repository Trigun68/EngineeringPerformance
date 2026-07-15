import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('References grid stability', () => {
  test('directory rows stay visible after load and tab switches', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/references/physical-volumes-kfo');
    await expect(page).toHaveURL(/\/references\/physical-volumes-kfo/, { timeout: 15_000 });

    const gridRows = page.locator('.ag-center-cols-container .ag-row');
    await expect(gridRows.first()).toBeVisible({ timeout: 20_000 });

    const countAfterLoad = await gridRows.count();
    expect(countAfterLoad).toBeGreaterThan(0);

    await page.waitForTimeout(3000);
    expect(await gridRows.count()).toBe(countAfterLoad);
    await expect(gridRows.first()).toBeVisible();

    await page.getByRole('link', { name: /Трудоёмкость/ }).click();
    await expect(page).toHaveURL(/\/references\/labor-intensities/, { timeout: 10_000 });
    await expect(gridRows.first()).toBeVisible({ timeout: 20_000 });
    const laborCount = await gridRows.count();
    expect(laborCount).toBeGreaterThan(0);

    await page.waitForTimeout(2000);
    expect(await gridRows.count()).toBe(laborCount);

    await page.getByRole('link', { name: /Номенклатура физических объёмов – КФО/ }).click();
    await expect(page).toHaveURL(/\/references\/physical-volumes-kfo/, { timeout: 10_000 });
    await expect(gridRows.first()).toBeVisible({ timeout: 20_000 });
    expect(await gridRows.count()).toBeGreaterThan(0);

    await page.getByRole('link', { name: 'Проекты' }).click();
    await page.getByRole('link', { name: 'Справочники' }).click();
    await expect(page).toHaveURL(/\/references\//, { timeout: 15_000 });
    await expect(gridRows.first()).toBeVisible({ timeout: 20_000 });
    expect(await gridRows.count()).toBeGreaterThan(0);
  });
});
