import { expect, type Page } from '@playwright/test';

export const ADMIN_USER = { username: 'admin', password: 'admin' };
export const OBSERVER_USER = { username: 'observer', password: 'observer' };

const USER_MENU = '[aria-label="Меню пользователя"]';

async function fillAndSubmitLogin(
  page: Page,
  credentials: { username: string; password: string },
): Promise<void> {
  await expect(page.getByText('Авторизация')).toBeVisible({ timeout: 20_000 });
  await page.getByLabel('Имя пользователя').fill(credentials.username);
  await page.getByLabel('Пароль').fill(credentials.password);
  const loginBtn = page.getByRole('button', { name: 'Войти' });
  // Кнопка может быть disabled до blur полей — клик после небольшого ожидания + force.
  await page.getByLabel('Пароль').blur().catch(() => undefined);
  await loginBtn.click({ timeout: 30_000, force: true });

  // Soft MFA setup for SuperAdmin/Admin — dismiss so e2e can proceed.
  const mfaSkip = page.getByTestId('mfa-setup-skip');
  if (await mfaSkip.isVisible({ timeout: 3_000 }).catch(() => false)) {
    await mfaSkip.click();
  }

  await expect(page.locator(USER_MENU)).toBeVisible({ timeout: 30_000 });
}

async function loginWithCredentials(
  page: Page,
  credentials: { username: string; password: string },
): Promise<void> {
  const authHeading = page.getByText('Авторизация');
  if (await authHeading.isVisible().catch(() => false)) {
    await fillAndSubmitLogin(page, credentials);
    return;
  }

  await page.context().clearCookies();
  await page.goto('/', { waitUntil: 'domcontentloaded' });

  const userMenu = page.locator(USER_MENU);
  await Promise.race([
    authHeading.waitFor({ state: 'visible', timeout: 30_000 }),
    userMenu.waitFor({ state: 'visible', timeout: 30_000 }),
  ]);

  if (await userMenu.isVisible().catch(() => false)) {
    await logoutViaUserMenu(page);
  }

  await fillAndSubmitLogin(page, credentials);
}

export async function loginAsAdmin(page: Page): Promise<void> {
  await page.context().clearCookies();
  await page.goto('/', { waitUntil: 'domcontentloaded' });

  const menuVisible = await page
    .locator(USER_MENU)
    .waitFor({ state: 'visible', timeout: 30_000 })
    .then(() => true)
    .catch(() => false);

  if (menuVisible) {
    await expect(page).toHaveURL(/\/$/);
    return;
  }

  await loginWithCredentials(page, ADMIN_USER);
  await expect(page).toHaveURL(/\/$/);
}

export async function loginAsObserver(page: Page): Promise<void> {
  await loginWithCredentials(page, OBSERVER_USER);
}

export async function logoutViaUserMenu(page: Page): Promise<void> {
  await page.locator(USER_MENU).click();
  const logoutResponse = page.waitForResponse(
    (response) => response.url().includes('/auth/logout') && response.status() === 204,
  );
  await page.getByRole('menuitem', { name: 'Выход' }).click();
  await logoutResponse;
  await expect(page.getByText('Авторизация')).toBeVisible({ timeout: 15_000 });
}

/**
 * Открывает проект с данными (по умолчанию 25-310), иначе первую строку списка.
 */
export async function openPreferredProject(
  page: Page,
  projectCode = '25-310',
): Promise<number | null> {
  await page.goto('/projects');
  await page.waitForSelector('.ag-center-cols-container .ag-row', { timeout: 15_000 }).catch(() => null);
  const preferred = page
    .locator('.ag-center-cols-container .ag-row')
    .filter({ hasText: projectCode });
  const target =
    (await preferred.count()) > 0
      ? preferred.first()
      : page.locator('.ag-center-cols-container .ag-row').first();
  if (!(await target.isVisible().catch(() => false))) {
    return null;
  }
  await target.dblclick();
  await expect(page).toHaveURL(/\/projects\/\d+/);
  await waitForWorkingOverlayHidden(page);
  const match = page.url().match(/\/projects\/(\d+)/);
  return match ? Number(match[1]) : null;
}

/** @deprecated Prefer openPreferredProject for data-rich seeds. */
export async function openFirstProject(page: Page): Promise<number | null> {
  return openPreferredProject(page);
}

/** Ждёт исчезновения «Работаю…», чтобы клики не перехватывались Backdrop. */
export async function waitForWorkingOverlayHidden(page: Page, timeout = 60_000): Promise<void> {
  // Несколько WorkingOverlay могут быть смонтированы одновременно (layout + page).
  await expect(page.locator('[data-testid="working-overlay"]:visible')).toHaveCount(0, { timeout });
}

/**
 * Сбрасывает сохранённые layout/filter prefs гридов (ключи, содержащие needles).
 * Нужно, когда прошлые E2E спрятали колонки через applyColumnState.
 */
export async function clearMatchingGridLayouts(page: Page, needles: string[]): Promise<void> {
  await page.evaluate(async (keys) => {
    const prefsRes = await fetch('/api/users/me/preferences', { credentials: 'include' });
    if (prefsRes.ok) {
      const prefs = (await prefsRes.json()) as Record<string, string>;
      const updates: Record<string, string> = {};
      for (const key of Object.keys(prefs ?? {})) {
        if (keys.some((n) => key.includes(n))) {
          updates[key] = '[]';
        }
      }
      if (Object.keys(updates).length > 0) {
        await fetch('/api/users/me/preferences', {
          method: 'PUT',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ preferences: updates }),
        });
      }
    }

    // Legacy userSettings: grid-{gridId}
    for (const needle of keys) {
      const legacyKeys = [
        `grid-${needle}`,
        `grid-ref-${needle}`,
        needle.startsWith('grid-') ? needle : `grid-${needle}`,
      ];
      for (const legacyKey of legacyKeys) {
        await fetch(`/api/users/me/settings/${encodeURIComponent(legacyKey)}`, {
          method: 'PUT',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ value: '[]' }),
        }).catch(() => undefined);
      }
    }
  }, needles);
}

/** Горизонтальный скролл viewport AG Grid (колонки за краем при virtualisation). */
export async function scrollAgGridToColumn(page: Page, colId: string, gridId?: string): Promise<void> {
  if (gridId) {
    await page.evaluate(
      ({ id, column }) => {
        const api = (window as unknown as { __epGridApis?: Record<string, { ensureColumnVisible?: (c: string) => void }> })
          .__epGridApis?.[id];
        api?.ensureColumnVisible?.(column);
      },
      { id: gridId, column: colId },
    );
  }

  const header = page.locator(`.ag-header-cell[col-id="${colId}"]`).first();
  if (await header.isVisible().catch(() => false)) {
    await header.evaluate((el) => el.scrollIntoView({ inline: 'center', block: 'nearest' }));
    return;
  }

  // Сразу в конец, затем пошагово назад — быстрее, чем только вперёд.
  await page.locator('.ag-body-horizontal-scroll-viewport').first().evaluate((el) => {
    el.scrollLeft = el.scrollWidth;
  }).catch(() => undefined);
  await page.locator('.ag-header-viewport').first().evaluate((el) => {
    el.scrollLeft = el.scrollWidth;
  }).catch(() => undefined);

  if (await header.isVisible().catch(() => false)) {
    await header.evaluate((el) => el.scrollIntoView({ inline: 'center', block: 'nearest' }));
    return;
  }

  for (let i = 0; i < 16; i++) {
    await page.locator('.ag-body-horizontal-scroll-viewport').first().evaluate((el) => {
      el.scrollLeft = Math.max(0, el.scrollLeft - el.clientWidth * 0.7);
    }).catch(() => undefined);
    await page.locator('.ag-header-viewport').first().evaluate((el) => {
      el.scrollLeft = Math.max(0, el.scrollLeft - el.clientWidth * 0.7);
    }).catch(() => undefined);
    if (await header.isVisible().catch(() => false)) {
      await header.evaluate((el) => el.scrollIntoView({ inline: 'center', block: 'nearest' }));
      return;
    }
  }
}
