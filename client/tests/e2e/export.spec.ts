import { test, expect, type Route } from '@playwright/test';
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
    Projects: 1,
    ProjectAnalysisVersions: 2,
    ProjectDataUpload: 2,
    Assessment: 1,
    Planning: 1,
  },
};

const xlsxMime = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
const expectedFilename = 'Project_1_v1.xlsx';

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  });
}

test.describe('ProjectsPage export', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/auth/negotiate', (route) =>
      json(route, { expiresAt: new Date().toISOString(), user: mockUser }),
    );
    await page.route('**/api/auth/me', (route) => json(route, mockUser));
    await page.route('**/api/dictionaries/all', (route) => json(route, dictionaries));
    await page.route('**/api/projects/1/versions', (route) =>
      json(route, [
        { id: 1, projectId: 1, versionName: 'v1', name: 'Версия 1', direction: 0, isArchived: false },
      ]),
    );
    await page.route('**/api/projects/1/versions/1/page-data**', (route) => json(route, gridData));
  });

  test('toolbar export excel triggers download with correct filename and mime', async ({ page }) => {
    await page.route('**/api/projects/1/versions/1/export/excel**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: xlsxMime,
        headers: {
          'Content-Disposition': `attachment; filename="${expectedFilename}"`,
        },
        body: Buffer.from('PK-fake-xlsx'),
      });
    });

    const downloadPromise = page.waitForEvent('download');

    await page.goto('/projects/1/grids?versionId=1');
    await page.getByRole('button', { name: 'Экспорт Excel' }).click();

    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe(expectedFilename);

    const path = await download.path();
    expect(path).toBeTruthy();
  });

  test('shows alert on export 403', async ({ page }) => {
    await page.route('**/api/projects/1/versions/1/export/pdf**', (route) =>
      json(route, { message: 'Forbidden' }, 403),
    );

    await page.goto('/projects/1/grids?versionId=1');
    await page.getByRole('button', { name: 'Экспорт PDF' }).click();

    await expect(page.getByText('Нет прав на экспорт данных проекта.')).toBeVisible();
  });
});
