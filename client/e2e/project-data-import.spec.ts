import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loginAsAdmin, actionButton, ACTION_LABELS, waitForWorkingOverlayHidden } from './helpers';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SRC_DIR = process.env.E2E_SAMPLES_DIR ?? path.resolve(__dirname, '../../.src');

const SAMPLE_FILES = {
  physicalVolumes: path.join(SRC_DIR, 'Список работ 25-310.xlsx'),
  equipment: path.join(SRC_DIR, 'Оборудование.xlsx'),
  labor: path.join(SRC_DIR, '25-310 Трудоемкость прокта.xlsx'),
};

function requireSample(filePath: string): void {
  test.skip(!fs.existsSync(filePath), `Sample missing (gitignored .src): ${path.basename(filePath)}`);
}

async function openProjectWorkspace(page: import('@playwright/test').Page, projectId = 1) {
  await page.goto(`/projects/${projectId}`);
  await expect(page.getByRole('tab', { name: 'Прогнозирование' })).toBeVisible({ timeout: 20_000 });
}

async function importViaDataImportDialog(
  page: import('@playwright/test').Page,
  openButtonName: string,
  filePath: string,
  importUrlPart: string,
  successPattern: RegExp,
  timeoutMs = 180_000,
) {
  await expect(page.getByRole('button', { name: openButtonName, exact: true })).toBeEnabled({ timeout: 30_000 });
  await page.getByRole('button', { name: openButtonName, exact: true }).click({ force: true });
  const dialog = page.getByTestId('data-import-dialog');
  await expect(dialog).toBeVisible({ timeout: 15_000 });

  const fileChooserPromise = page.waitForEvent('filechooser');
  await dialog.getByRole('button', { name: ACTION_LABELS.selectFile, exact: true }).click();
  const fileChooser = await fileChooserPromise;
  await fileChooser.setFiles(filePath);

  await expect(dialog.getByText(/Файл:/i)).toBeVisible();

  const importResponse = page.waitForResponse(
    (response) =>
      response.url().includes(importUrlPart)
      && response.request().method() === 'POST'
      && response.status() === 200,
    { timeout: timeoutMs },
  );

  await dialog.getByRole('button', { name: 'Импортировать' }).click();
  await importResponse;

  await expect(dialog.getByRole('alert').filter({ hasText: successPattern })).toBeVisible({
    timeout: 15_000,
  });
  await dialog.locator('.MuiDialogActions-root').getByRole('button', { name: 'Закрыть' }).click();
  await expect(dialog).not.toBeVisible();
}

test.describe('Project data import (.src samples)', () => {
  test.use({ viewport: { width: 1600, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('forecast: import physical volumes from .src sample', async ({ page }) => {
    requireSample(SAMPLE_FILES.physicalVolumes);
    test.setTimeout(240_000);
    await openProjectWorkspace(page);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible({ timeout: 20_000 });
    await expect(actionButton(page, 'importPhysicalVolumes')).toBeEnabled({ timeout: 20_000 });

    await importViaDataImportDialog(
      page,
      ACTION_LABELS.importPhysicalVolumes,
      SAMPLE_FILES.physicalVolumes,
      '/import/physical-volumes',
      /Импортировано записей:\s*[1-9]/,
    );
  });

  test('forecast: import equipment from .src sample', async ({ page }) => {
    requireSample(SAMPLE_FILES.equipment);
    test.setTimeout(180_000);
    await openProjectWorkspace(page);
    await page.getByRole('tab', { name: 'Прогнозирование' }).click();
    await expect(page.getByRole('button', { name: /Версия \(план\)/ })).toBeVisible({ timeout: 20_000 });
    await expect(actionButton(page, 'importEquipment')).toBeEnabled({ timeout: 20_000 });

    await importViaDataImportDialog(
      page,
      ACTION_LABELS.importEquipment,
      SAMPLE_FILES.equipment,
      '/import/equipment',
      /Импортировано записей:\s*[1-9]/,
      120_000,
    );
  });

  test('fact: labor preview and import from .src sample', async ({ page }) => {
    requireSample(SAMPLE_FILES.labor);
    test.setTimeout(120_000);
    await openProjectWorkspace(page);
    await page.getByRole('tab', { name: 'Оценка факта' }).click();
    await expect(actionButton(page, 'importLabor')).toBeEnabled({ timeout: 20_000 });

    await actionButton(page, 'importLabor').click({ force: true });
    const dialog = page.getByTestId('data-import-dialog');
    await expect(dialog).toBeVisible();

    const previewResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/import/labor-intensities/preview')
        && response.request().method() === 'POST'
        && response.status() === 200,
    );

    const fileChooserPromise = page.waitForEvent('filechooser');
    await dialog.getByRole('button', { name: ACTION_LABELS.selectFile, exact: true }).click();
    await (await fileChooserPromise).setFiles(SAMPLE_FILES.labor);
    await previewResponse;

    await expect(dialog.getByText(/Строк в файле:\s*[1-9]/)).toBeVisible();
    await expect(dialog.getByText(/Не найдено в справочнике:\s*\d+/)).toBeVisible();
    await expect(dialog.getByText(/Не найдено в справочнике:\s*1008/)).toHaveCount(0);

    const importResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/import/labor-intensities')
        && !response.url().includes('/preview')
        && response.request().method() === 'POST'
        && response.status() === 200,
      { timeout: 90_000 },
    );
    await dialog.getByRole('button', { name: 'Импортировать' }).click();
    await importResponse;

    await expect(dialog.getByText(/Импортировано записей:\s*[1-9]/)).toBeVisible();
    await dialog.locator('.MuiDialogActions-root').getByRole('button', { name: 'Закрыть' }).click();
    await expect(dialog).not.toBeVisible();
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.getByRole('tab', { name: 'Итого', exact: true }).click();
    await waitForWorkingOverlayHidden(page, 90_000);
    await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 30_000 });

    const hoursTotal = await page.evaluate(() => {
      const w = window as Window & {
        __epGridApis?: Record<string, { forEachNode: (cb: (node: { data?: { factLaborHours?: number | null } }) => void) => void }>;
      };
      const api = w.__epGridApis?.['fact-performance'];
      if (!api) {
        return 0;
      }
      let sum = 0;
      api.forEachNode((node) => {
        const hours = node.data?.factLaborHours;
        if (typeof hours === 'number' && hours > 0) {
          sum += hours;
        }
      });
      return sum;
    });
    expect(hoursTotal).toBeGreaterThan(0);
  });
});
