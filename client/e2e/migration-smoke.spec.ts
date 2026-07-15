import { test, expect } from '@playwright/test';
import { loginAsAdmin, openFirstProject } from './helpers';

test.describe('Migration UAT smoke (Playwright)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('login and app shell navigation', async ({ page }) => {
    await expect(page.getByRole('link', { name: 'Главная' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Проекты' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Справочники' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Справка' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Администрирование' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Настройки' })).toBeVisible();
  });

  test('dashboard — methodology and sigma chart', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('Главная').first()).toBeVisible();
    await expect(page.getByText('Методология оценки проектов')).toBeVisible();
    await expect(page.getByText('Интерпретация стандартного отклонения (σ)')).toBeVisible();
    await expect(page.getByRole('img', { name: 'Интерпретация стандартного отклонения' })).toBeVisible();
  });

  test('projects list loads grid', async ({ page }) => {
    await page.goto('/projects');
    await expect(page.getByText('Проекты').first()).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });

  test('references — physical volumes BER directory', async ({ page }) => {
    await page.goto('/references/physical-volumes-ber');
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
    await expect(page.getByRole('link', { name: 'Нормы физобъёмов (IIC)' })).toBeVisible();
  });

  test('help page — interpretation guide', async ({ page }) => {
    await page.goto('/help');
    await expect(page.getByRole('heading', { name: 'Справка по показателям' })).toBeVisible();
    await expect(page.getByText('Z-оценка по направлениям IIC')).toBeVisible();
  });

  test('audit log page', async ({ page }) => {
    await page.goto('/admin/user-actions');
    await expect(page.getByRole('heading', { name: 'Журнал действий пользователей' })).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });

  test('settings — users directory', async ({ page }) => {
    await page.goto('/settings/users');
    await expect(page.getByRole('heading', { name: 'Пользователи' })).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });

  test('access rights matrix', async ({ page }) => {
    await page.goto('/settings/access-rights');
    await expect(page.getByRole('heading', { name: 'Матрица прав доступа' })).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });

  test('project detail — data and analytics tabs', async ({ page }) => {
    const projectId = await openFirstProject(page);
    test.skip(projectId === null, 'No projects in database');

    for (const tabName of [
      'Прогнозирование',
      'Оценка факта',
      'Сравнение версий',
      'Отчёты',
    ]) {
      await page.getByRole('tab', { name: tabName, exact: true }).first().click();
      await expect(page.getByRole('tab', { name: tabName, exact: true }).first()).toHaveAttribute('aria-selected', 'true');
    }
  });

  test('project compare — sub-tabs', async ({ page }) => {
    const projectId = await openFirstProject(page);
    test.skip(projectId === null, 'No projects in database');

    await page.getByRole('tab', { name: 'Сравнение' }).click();
    await page.getByRole('tab', { name: 'По объектам' }).click();
    await expect(page.getByRole('tab', { name: 'По объектам' })).toHaveAttribute('aria-selected', 'true');
    await page.getByRole('tab', { name: 'Детально' }).click();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });
  });
});
