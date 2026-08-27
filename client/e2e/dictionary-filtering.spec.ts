import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Dictionary filtering', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('excel-style filter shows search, select all and apply', async ({ page }) => {
    await page.goto('/references/units-of-measurement');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    const initialRows = await page.locator('.ag-center-cols-container .ag-row').count();
    expect(initialRows).toBeGreaterThan(0);

    const nameHeader = page.locator('.ag-header-cell').filter({ hasText: 'Наименование' });
    await nameHeader.locator('.ag-header-cell-filter-button').click();

    await expect(page.getByPlaceholder('Поиск...')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Выбрать все' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Снять все' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Применить' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Сбросить' })).toBeVisible();

    await page.getByPlaceholder('Поиск...').fill('несуществующее_значение_xyz');
    await expect(page.getByText('Нет совпадений')).toBeVisible();

    await page.getByRole('button', { name: 'Сбросить' }).click();
    await nameHeader.locator('.ag-header-cell-filter-button').click();
    await page.getByRole('button', { name: 'Снять все' }).click();
    await page.getByRole('button', { name: 'Применить' }).click();

    await expect(page.locator('.ag-center-cols-container .ag-row')).toHaveCount(0, { timeout: 10_000 });
  });

  test('column settings dialog lists all configured columns', async ({ page }) => {
    await page.goto('/references/units-of-measurement');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    await page.getByRole('button', { name: 'Настройка столбцов' }).click();
    await expect(page.getByRole('heading', { name: 'Настройка столбцов' })).toBeVisible();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText('Наименование')).toBeVisible();
    await expect(dialog.getByText('Архив')).toBeVisible();
  });

  test('dictionary page scrolls inside grid, not the whole page', async ({ page }) => {
    await page.goto('/references/physical-volumes-kfo');
    await page.waitForSelector('.ag-body-viewport', { timeout: 15_000 });

    const pageScroll = await page.evaluate(() => ({
      docScrollHeight: document.documentElement.scrollHeight,
      windowHeight: window.innerHeight,
    }));
    expect(pageScroll.docScrollHeight).toBeLessThanOrEqual(pageScroll.windowHeight + 2);

    const gridScroll = await page.locator('.ag-body-viewport').first().evaluate((el) => ({
      scrollHeight: el.scrollHeight,
      clientHeight: el.clientHeight,
    }));
    expect(gridScroll.scrollHeight).toBeGreaterThan(gridScroll.clientHeight);
  });

  test('KFO tree grid keeps header visible when scrolling body', async ({ page }) => {
    await page.goto('/references/physical-volumes-kfo');
    await page.waitForSelector('.ag-body-viewport', { timeout: 15_000 });

    const viewport = page.locator('.ag-body-viewport').first();
    const header = page.locator('.ag-header').first();

    await viewport.evaluate((el) => {
      el.scrollTop = 400;
    });

    const headerBox = await header.boundingBox();
    const viewportBox = await viewport.boundingBox();
    expect(headerBox).not.toBeNull();
    expect(viewportBox).not.toBeNull();
    expect(headerBox!.y).toBeLessThan(viewportBox!.y);
  });

  test('KFO classifier column filter lists formatted labels not raw ids', async ({ page }) => {
    await page.goto('/references/physical-volumes-kfo');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    const header = page.locator('.ag-header-cell').filter({ hasText: 'Классификатор (КФО)' });
    await expect(header).toBeVisible();
    await header.locator('.ag-header-cell-filter-button').click();

    await expect(page.getByPlaceholder('Поиск...')).toBeVisible();
    const labels = page.locator('.MuiFormControlLabel-label');
    await expect(labels.first()).toBeVisible({ timeout: 10_000 });

    const texts = await labels.allTextContents();
    expect(texts.length).toBeGreaterThan(0);
    // Сырые числовые id классификатора не должны быть единственной подписью пункта
    const numericOnly = texts.filter((t) => /^\d+$/.test(t.trim()));
    expect(numericOnly.length).toBe(0);

    // Есть хотя бы одна отформатированная группа IIC или «(Пусто)»
    expect(
      texts.some((t) => t.includes('IIC-') || t.includes('(Пусто)') || t.includes('—')),
    ).toBe(true);

    await page.getByRole('button', { name: 'Сбросить' }).click();
  });

  test('BER tree grid keeps header visible when scrolling body', async ({ page }) => {
    await page.goto('/references/physical-volumes-ber');
    await page.waitForSelector('.ag-body-viewport', { timeout: 15_000 });

    const viewport = page.locator('.ag-body-viewport').first();
    const header = page.locator('.ag-header').first();

    await viewport.evaluate((el) => {
      el.scrollTop = 400;
    });

    const headerBox = await header.boundingBox();
    const viewportBox = await viewport.boundingBox();
    expect(headerBox).not.toBeNull();
    expect(viewportBox).not.toBeNull();
    expect(headerBox!.y).toBeLessThan(viewportBox!.y);
  });
});
