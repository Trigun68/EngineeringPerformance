import { test, expect, type Route } from '@playwright/test';
import { ACTION_LABELS } from '../../src/constants/actionLabels';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const gridData = JSON.parse(readFileSync(join(__dirname, '../fixtures/grid-data.json'), 'utf8'));
const dictionaries = JSON.parse(readFileSync(join(__dirname, '../fixtures/dictionaries.json'), 'utf8'));

const mockUser = {
  id: 1,
  username: 'pm',
  displayName: 'Project Manager',
  roleId: 2,
  roleName: 'Admin',
  isActive: true,
  permissions: {
    Dashboard: 1,
    Projects: 2,
    ProjectAnalysisVersions: 2,
    ProjectDataUpload: 2,
    Assessment: 1,
    Planning: 1,
  },
};

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  });
}

test.describe('ProjectsPage 9 grids E2E', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/auth/negotiate', (route) => json(route, { expiresAt: new Date().toISOString(), user: mockUser }));
    await page.route('**/api/auth/me', (route) => json(route, mockUser));
    await page.route('**/api/dictionaries/all', (route) => json(route, dictionaries));
    await page.route('**/api/projects/1/versions', (route) =>
      json(route, [
        { id: 1, projectId: 1, versionName: 'v1', name: 'Версия 1', direction: 0, isArchived: false },
        { id: 2, projectId: 1, versionName: 'v2', name: 'Версия 2', direction: 0, isArchived: false },
      ]),
    );
    await page.route('**/api/projects/1/versions/1/page-data/detail**', (route) =>
      json(route, {
        forecastDetail: gridData.forecast.detail,
        factDetail: gridData.fact.detail,
        compareDetail: gridData.compare?.detail ?? [],
      }),
    );
    await page.route('**/api/projects/1/versions/1/page-data**', (route) => json(route, gridData));
  });

  test('loads grid and shows context menu export for manager', async ({ page }) => {
    await page.goto('/projects/1/grids?versionId=1');
    await expect(page.getByText('Данные проекта #1')).toBeVisible();
    await page.waitForSelector('.ag-root', { timeout: 15_000 });

    const row = page.locator('.ag-center-cols-container .ag-row').first();
    await row.click({ button: 'right' });
    await expect(page.getByText(ACTION_LABELS.exportExcel)).toBeVisible();
    await expect(page.getByText(ACTION_LABELS.importExcel)).toBeVisible();
  });

  test('export excel triggers download', async ({ page }) => {
    await page.route('**/api/projects/1/versions/1/export/excel**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        headers: { 'Content-Disposition': 'attachment; filename="отчет_версия_-_P1_Проект_(v1).xlsx"' },
        body: Buffer.from('PK-fake-xlsx'),
      });
    });

    let downloadStarted = false;
    page.on('download', () => {
      downloadStarted = true;
    });

    await page.goto('/projects/1/grids?versionId=1');
    await page.waitForSelector('.ag-row', { timeout: 15_000 });

    const row = page.locator('.ag-center-cols-container .ag-row').first();
    await row.click({ button: 'right' });
    await page.getByText(ACTION_LABELS.exportExcel).click();

    await expect.poll(() => downloadStarted).toBe(true);
  });
});
