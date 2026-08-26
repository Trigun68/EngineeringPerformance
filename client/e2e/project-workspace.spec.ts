import { test, expect } from '@playwright/test';
import { loginAsAdmin, actionButton } from './helpers';

test.describe('Project Workspace - WPF Style', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('projects list has search and edit controls without export actions', async ({ page }) => {
    await page.goto('/projects');

    await expect(page.getByLabel('Поиск по проектам')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Добавить' })).toBeVisible();

    const editSelectedButton = page.getByRole('button', { name: 'Редактировать' });
    await expect(editSelectedButton).toBeVisible();
    await expect(editSelectedButton).toBeDisabled();

    await expect(actionButton(page, 'exportExcel')).toHaveCount(0);
    await expect(actionButton(page, 'exportExcelSelected')).toHaveCount(0);
    await expect(actionButton(page, 'exportPdf')).toHaveCount(0);
    await expect(actionButton(page, 'importExcel')).toHaveCount(0);

    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
    const firstCode = (await firstRow.locator('[col-id="code"]').first().textContent())?.trim() ?? '';
    await firstRow.click();
    await expect(editSelectedButton).toBeEnabled();

    if (firstCode) {
      await page.getByLabel('Поиск по проектам').fill(firstCode);
      await expect(page.locator('.ag-center-cols-container .ag-row')).toHaveCount(1);
      await expect(page.locator('.ag-center-cols-container .ag-row').first()).toContainText(firstCode);
    }
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

  test('characteristics button opens characteristics dialog', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Click characteristics button after project header is fully loaded
    const characteristicsButton = page.getByRole('button', { name: 'Характеристики' });
    await expect(characteristicsButton).toBeEnabled();
    await characteristicsButton.click();

    // Dialog should open
    await expect(page.getByText('Характеристики проекта')).toBeVisible();
    await page.getByRole('button', { name: 'Закрыть' }).click();
  });

  test('shows main tabs: Прогнозирование, Оценка факта, Сравнение версий, Отчет', async ({ page }) => {
    // Navigate to first project
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    // Check for main tabs (WPF-style + visual report)
    await expect(page.getByRole('tab', { name: 'Прогнозирование' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Оценка факта' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Сравнение версий' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Отчет' })).toBeVisible();
  });

  test('report tab loads performance report panel', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 });
    await page.locator('.ag-center-cols-container .ag-row').first().dblclick();
    await page.waitForURL(/\/projects\/\d+/);

    await page.getByRole('tab', { name: 'Отчет' }).click();
    await expect(page.getByTestId('performance-report-panel')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole('button', { name: /План:/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Факт:/ })).toHaveCount(0);
    await expect(actionButton(page, 'exportPdf')).toBeVisible();
    await expect(page.getByTestId('report-plan-z')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('report-fact-z')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('report-directions-table')).toBeVisible({ timeout: 30_000 });
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
    await expect(actionButton(page, 'importPhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'importEquipment')).toBeVisible();
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
    await expect(actionButton(page, 'importPhysicalVolumes')).toBeVisible();
    await expect(actionButton(page, 'importEquipment')).toBeVisible();
    await expect(actionButton(page, 'importLabor')).toBeVisible();
  });
});
