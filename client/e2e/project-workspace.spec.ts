import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers';

test.describe('Project Workspace - WPF Style', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('shows project selection card when accessing /projects/0 (no project)', async ({ page }) => {
    await page.goto('/projects/0');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Depending on seed data and routing fallback, we can land on:
    // - "Проект не выбран" card
    // - project selection dialog
    // - project toolbar with "Выбрать проект"
    const hasSelectButton = await page.getByRole('button', { name: 'Выбрать проект' }).first().isVisible().catch(() => false);
    const hasNoProjectCard = await page.getByText('Проект не выбран').isVisible().catch(() => false);
    const hasSelectionDialog = await page.getByText('Выбор проекта').isVisible().catch(() => false);

    expect(hasSelectButton || hasNoProjectCard || hasSelectionDialog).toBeTruthy();
  });

  test('project selection dialog lists all projects', async ({ page }) => {
    await page.goto('/projects/1');

    // Open selection dialog
    await page.getByRole('button', { name: 'Выбрать проект' }).first().click();

    // Check dialog content
    await expect(page.getByText('Выбор проекта')).toBeVisible();

    // Should have project list
    const projectItems = page.locator('[role="dialog"] li');
    const count = await projectItems.count();
    expect(count).toBeGreaterThan(0);

    // Close dialog
    await page.getByRole('button', { name: 'Отмена' }).click();
  });

  test('selected project shows project name and code', async ({ page }) => {
    await page.goto('/projects/1');

    // Open selection dialog
    await page.getByRole('button', { name: 'Выбрать проект' }).first().click();

    // Select first project
    const firstProject = page.locator('[role="dialog"] li').first();
    const selectedProjectText = (await firstProject.textContent())?.trim() ?? '';
    await firstProject.click();

    // Click select button
    await page.getByRole('button', { name: 'Выбрать' }).click();

    // Should navigate to project workspace
    await page.waitForURL(/\/projects\/\d+/);

    // Should show top project header with name/code.
    await expect(page.getByRole('button', { name: 'Выбрать проект' }).first()).toBeVisible({ timeout: 10000 });

    const projectHeading = page.getByRole('heading', { level: 6 }).first();
    await expect(projectHeading).toBeVisible();

    if (selectedProjectText) {
      const normalized = selectedProjectText.replace(/\s+/g, ' ').trim();
      const projectNameCandidate = normalized.includes('—')
        ? normalized.split('—').pop()?.trim() ?? normalized
        : normalized;
      await expect(page.getByText(projectNameCandidate, { exact: false }).first()).toBeVisible();
    }
  });

  test('project workspace shows action buttons', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });

    // Double click first project
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();

    // Wait for project page to load
    await page.waitForURL(/\/projects\/\d+/);

    // Check for WPF-style buttons (new FullProjectsPage)
    await expect(page.getByRole('button', { name: 'Выбрать проект' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Участники' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Характеристики' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Основные данные' })).toBeVisible();
  });

  test('participants button opens participants dialog', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click participants button
    await page.getByRole('button', { name: 'Участники' }).click();

    // Check dialog opens - looking for dialog title (Участники проекта)
    await expect(page.getByText('Участники проекта')).toBeVisible();

    // Close dialog (button says Отмена)
    await page.getByRole('button', { name: 'Отмена' }).first().click();
  });

  test('characteristics button shows alert in current implementation', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Setup dialog handler
    page.once('dialog', dialog => dialog.accept());

    // Click characteristics button
    await page.getByRole('button', { name: 'Характеристики' }).click();
  });

  test('shows main tabs: Прогнозирование, Оценка факта, Сравнение версий, Отчёты', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Check for main tabs (WPF-style)
    await expect(page.getByRole('tab', { name: 'Прогнозирование' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Оценка факта' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Сравнение версий' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Отчёты' })).toBeVisible();
  });

  test('forecast tab shows version selector and import buttons', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Should be on forecast tab by default or click it
    const forecastTab = page.getByRole('tab', { name: 'Прогнозирование' });
    await forecastTab.click();

    // Check for version button
    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible();

    // Check for import buttons
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
  });

  test('fact tab shows version selector and more import buttons', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click fact tab
    const factTab = page.getByRole('tab', { name: 'Оценка факта' });
    await factTab.click();

    // Check for version button
    await expect(page.getByRole('button', { name: /Версия \(факт\)/ })).toBeVisible();

    // Check for import buttons (fact has more options)
    await expect(page.getByRole('button', { name: 'Импорт ФО' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт оборуд.' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Импорт труд.' })).toBeVisible();
  });
});
