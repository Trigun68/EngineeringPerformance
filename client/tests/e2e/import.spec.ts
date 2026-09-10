import { test, expect, type Route } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const fixturePath = path.join(__dirname, '../fixtures/test_import.xlsx');

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
    ProjectEquipments: 1,
    ProjectPhysicalVolumes: 1,
    ProjectLaborIntensities: 1,
  },
};

const mockDictionaries = {
  physicalVolumeTypes: [{ id: 1, name: 'Norm Type 1', code: 'NT1' }],
  physicalVolumes: [],
  equipments: [],
  laborIntensities: [],
  iicWorkDirections: [{ id: 1, name: 'Направление А', code: 'DA' }],
  iicWorkDirectionNorms: [],
  unitsOfMeasurement: [{ id: 1, name: 'шт' }],
  docSets: [],
  projectCharacteristics: [],
};

const mockProject = {
  id: 1,
  code: 'PRJ-1',
  name: 'Тестовый проект',
  workNumber: '',
  contractNumber: '',
  calendarPlanNumber: '',
  type: '',
  customer: '',
  contractor: '',
  gip: '',
  workStatus: '',
  workStart: null,
  program: '',
};

const mockVersions = [
  { id: 1, projectId: 1, versionName: 'v1', name: 'Версия 1', direction: 0, isArchived: false },
];

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  });
}

test.describe('Import equipment E2E', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/auth/negotiate', (route) => json(route, { expiresAt: new Date().toISOString(), user: mockUser }));
    await page.route('**/api/auth/me', (route) => json(route, mockUser));
    await page.route('**/api/dictionaries/all', (route) => json(route, mockDictionaries));
    await page.route('**/api/projects', (route) => json(route, [mockProject]));
    await page.route('**/api/projects/1/versions', (route) => json(route, mockVersions));
    await page.route('**/api/projects/1/permissions', (route) => json(route, []));
    await page.route('**/api/projects/1/versions/1/physical-volumes**', (route) =>
      json(route, { items: [], totalCount: 0, page: 1, pageSize: 100 }),
    );
    await page.route('**/api/projects/1/versions/1/labor-intensities**', (route) =>
      json(route, { items: [], totalCount: 0, page: 1, pageSize: 100 }),
    );
    await page.route('**/api/projects/1/versions/1/equipment**', (route) =>
      json(route, { items: [], totalCount: 0, page: 1, pageSize: 100 }),
    );
    await page.route('**/api/projects/1/versions/1/import/equipment/iterations', (route) => json(route, []));
  });

  test('should import equipment and display row errors for invalid data', async ({ page }) => {
    let importCalled = false;

    await page.route('**/api/projects/1/versions/1/import/equipment', async (route) => {
      if (route.request().method() === 'POST') {
        importCalled = true;
        await json(route, {
          dataType: 'equipment',
          versionId: 1,
          importedRows: 1,
          iterationId: 42,
          details: 'Загружено: 1 элементов',
          rowErrors: [
            { rowNumber: 3, message: 'Unit not found' },
            { rowNumber: 4, message: 'Direction not found' },
          ],
        });
        return;
      }
      await route.continue();
    });

    await page.goto('/projects/1');
    await expect(page.getByText('PRJ-1')).toBeVisible();

    await page.getByRole('tab', { name: 'Импорт', exact: true }).click();

    const importButton = page.getByRole('button', { name: /выбрать excel/i });
    await expect(importButton).toBeEnabled();

    await page.getByRole('combobox').click();
    await page.getByRole('option', { name: 'Оборудование' }).click();

    const fileChooserPromise = page.waitForEvent('filechooser');
    await importButton.click();
    const fileChooser = await fileChooserPromise;
    await fileChooser.setFiles(fixturePath);

    await expect(page.getByText(/файл:/i)).toBeVisible();

    const confirmButton = page.getByRole('button', { name: /подтвердить импорт/i });
    await expect(confirmButton).toBeEnabled();
    await confirmButton.click();

    await expect(page.getByRole('alert', { name: /ошибки строк импорта/i })).toBeVisible();
    await expect(page.getByText(/строка 3: unit not found/i)).toBeVisible();
    await expect(page.getByText(/строка 4: direction not found/i)).toBeVisible();
    await expect(page.getByText(/импортировано: 1/i)).toBeVisible();
    expect(importCalled).toBe(true);
  });
});
