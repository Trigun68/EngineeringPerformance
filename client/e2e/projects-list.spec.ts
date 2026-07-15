import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Projects list page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('toolbar shows Edit and archive checkbox', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    await expect(page.getByRole('button', { name: 'Добавить' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Редактировать' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить' })).toBeVisible();
    await expect(page.getByText('Отображать архивные элементы')).toBeVisible();
  });

  test('grid fills viewport and keeps header on scroll', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-body-viewport', { timeout: 15_000 });

    const viewport = page.locator('.ag-body-viewport').first();
    const header = page.locator('.ag-header').first();

    await viewport.evaluate((el) => {
      el.scrollTop = 200;
    });

    const headerBox = await header.boundingBox();
    const viewportBox = await viewport.boundingBox();
    expect(headerBox).not.toBeNull();
    expect(viewportBox).not.toBeNull();
    expect(headerBox!.y).toBeLessThan(viewportBox!.y);
  });
});
