import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

async function openFirstProject(page: import('@playwright/test').Page) {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
  await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
  await page.waitForURL(/\/projects\/\d+/);
}

test.describe('Fact and Compare modules - WPF parity', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('fact tab shows version button, six import buttons, and sub-tabs', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();

    await expect(page.getByRole('button', { name: /Версия \(факт\)/ })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт труд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Удалить труд.' })).toBeVisible();

    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'По объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('fact version picker dialog opens with title Версии факта', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await page.getByRole('button', { name: /Версия \(факт\)/ }).click();
    await expect(page.getByRole('dialog').getByText('Версии факта')).toBeVisible();
  });

  test('compare tab shows dual version pickers and differences checkbox', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Сравнение версий' }).click();

    await expect(page.getByRole('button', { name: /Версия А/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Версия Б/ })).toBeVisible();
    await expect(page.getByLabel('Показывать различия')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Таблица' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'График' })).toBeVisible();

    await expect(page.getByRole('tab', { name: 'Итого', exact: true })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'По объектам' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Детально' })).toBeVisible();
  });

  test('fact analysis dialog opens from fact tab', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    // Кнопка «Анализ версии» только на подвкладке «Итого».
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await page.getByRole('button', { name: /Анализ версии/ }).click();
    await expect(page.getByTestId('fact-analysis-dialog')).toBeVisible();
  });

  test('fact detail grouping via column drag shows group rows', async ({ page }) => {
    await openFirstProject(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await page.getByRole('tab', { name: 'Детально' }).click();
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    const groupDropZone = page
      .locator('.ag-column-drop-horizontal')
      .filter({ hasText: 'Перетащите сюда заголовок столбца для группировки' });
    await expect(groupDropZone).toBeVisible();

    const groupableHeader = page.getByRole('columnheader', { name: /Группа ФО IIC/ });
    await groupableHeader.dragTo(groupDropZone);

    await page.waitForSelector('.ag-row-group', { timeout: 10_000 });
  });
});
