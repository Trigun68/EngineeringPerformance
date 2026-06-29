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
  await page.getByRole('button', { name: 'Войти' }).click();
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

export async function openFirstProject(page: Page): Promise<number | null> {
  await page.goto('/projects');
  await page.waitForSelector('.ag-row', { timeout: 15_000 }).catch(() => null);
  const firstRow = page.locator('.ag-center-cols-container .ag-row').first();
  if (!(await firstRow.isVisible().catch(() => false))) {
    return null;
  }
  await firstRow.dblclick();
  await expect(page).toHaveURL(/\/projects\/\d+/);
  const match = page.url().match(/\/projects\/(\d+)/);
  return match ? Number(match[1]) : null;
}
