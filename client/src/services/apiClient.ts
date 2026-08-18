import axios, { type AxiosError, type AxiosInstance } from 'axios';
import { getApiErrorMessage } from './apiError';

type UnauthorizedHandler = () => void;
type ForbiddenHandler = (message?: string) => void;
type RemoteLogoutHandler = () => void;

/**
 * `dead` — сервер отверг refresh-токен (сессия закончилась);
 * `transient` — сеть/5xx, сессия жива и повтор возможен позже.
 */
export type RefreshOutcome = 'ok' | 'dead' | 'transient';

let onUnauthorized: UnauthorizedHandler | null = null;
let onForbidden: ForbiddenHandler | null = null;
let onRemoteLogout: RemoteLogoutHandler | null = null;
let refreshRequestPromise: Promise<RefreshOutcome> | null = null;

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
const SKIP_REFRESH_HEADER = 'X-Skip-Auth-Refresh';
const REFRESH_LOCK_KEY = 'engineering-performance-auth-refresh';
const REFRESH_LOCK_TTL_MS = 10_000;
const REFRESH_POLL_INTERVAL_MS = 150;
const authChannel = typeof BroadcastChannel === 'undefined'
  ? null
  : new BroadcastChannel('engineering-performance-auth');

authChannel?.addEventListener('message', (event: MessageEvent<unknown>) => {
  if (
    typeof event.data === 'object'
    && event.data !== null
    && 'type' in event.data
    && event.data.type === 'logout'
  ) {
    onRemoteLogout?.();
  }
});

// localStorage fallback для BroadcastChannel — работает при недоступности BC.
// Событие storage приходит только на window (не на document).
const AUTH_STORAGE_KEY = 'engineering-performance-auth';
window.addEventListener('storage', (event: StorageEvent) => {
  if (event.key !== AUTH_STORAGE_KEY) return;
  if (event.newValue === null) return;
  try {
    const msg = JSON.parse(event.newValue);
    if (msg?.type === 'logout') {
      onRemoteLogout?.();
    }
  } catch { /* malformed */ }
});

export function setUnauthorizedHandler(handler: UnauthorizedHandler): void {
  onUnauthorized = handler;
}

export function setForbiddenHandler(handler: ForbiddenHandler): void {
  onForbidden = handler;
}

export function setRemoteLogoutHandler(handler: RemoteLogoutHandler): void {
  onRemoteLogout = handler;
}

export function broadcastLogout(): void {
  authChannel?.postMessage({ type: 'logout' });
  // localStorage fallback для BroadcastChannel.
  try {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ type: 'logout', ts: Date.now() }));
  } catch { /* quota */ }
}

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

function shouldSkipRefresh(errorConfig: AxiosError['config']): boolean {
  if (!errorConfig) {
    return true;
  }

  if (typeof errorConfig.url === 'string' && errorConfig.url.includes('/auth/refresh')) {
    return true;
  }

  const headers = errorConfig.headers as Record<string, unknown> | undefined;
  if (!headers) {
    return false;
  }

  const rawHeader = headers[SKIP_REFRESH_HEADER] ?? headers[SKIP_REFRESH_HEADER.toLowerCase()];
  return String(rawHeader ?? '').toLowerCase() === '1';
}

async function runRefreshRequest(): Promise<RefreshOutcome> {
  try {
    await axios.post(`${API_BASE_URL}/auth/refresh`, null, {
      withCredentials: true,
      headers: {
        [SKIP_REFRESH_HEADER]: '1',
      },
    });
    return 'ok';
  } catch (error) {
    const status = axios.isAxiosError(error) ? error.response?.status : undefined;
    // Мёртвой сессию считаем только при явном отказе сервера.
    // Обрыв сети, рестарт backend, 5xx — временный сбой, разлогинивать нельзя.
    return status === 400 || status === 401 || status === 403 ? 'dead' : 'transient';
  }
}

function readRefreshLock(): { startedAt: number; outcome: RefreshOutcome | null } | null {
  try {
    const raw = localStorage.getItem(REFRESH_LOCK_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { startedAt?: unknown; outcome?: unknown };
    const startedAt = typeof parsed.startedAt === 'number' ? parsed.startedAt : 0;
    const outcome = typeof parsed.outcome === 'string' ? (parsed.outcome as RefreshOutcome) : null;
    return { startedAt, outcome };
  } catch {
    return null;
  }
}

/**
 * Захват лидерства на refresh между вкладками. localStorage не даёт атомарного CAS,
 * поэтому окно гонки остаётся — его закрывает grace-окно ротации на backend.
 */
function acquireRefreshLock(): boolean {
  const lock = readRefreshLock();
  if (lock && lock.outcome === null && Date.now() - lock.startedAt < REFRESH_LOCK_TTL_MS) {
    return false;
  }

  try {
    localStorage.setItem(REFRESH_LOCK_KEY, JSON.stringify({ startedAt: Date.now(), outcome: null }));
  } catch {
    // Приватный режим / переполнение квоты — работаем без межвкладочной координации.
  }
  return true;
}

function releaseRefreshLock(outcome: RefreshOutcome): void {
  try {
    localStorage.setItem(REFRESH_LOCK_KEY, JSON.stringify({ startedAt: Date.now(), outcome }));
  } catch {
    // см. acquireRefreshLock
  }
}

/** Ожидание результата refresh от вкладки-лидера. null — лидер не отчитался вовремя. */
async function waitForLeaderOutcome(): Promise<RefreshOutcome | null> {
  const deadline = Date.now() + REFRESH_LOCK_TTL_MS;
  while (Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, REFRESH_POLL_INTERVAL_MS));
    const lock = readRefreshLock();
    if (lock?.outcome) {
      return lock.outcome;
    }
    if (!lock) {
      return null;
    }
  }
  return null;
}

async function coordinateRefresh(): Promise<RefreshOutcome> {
  if (!acquireRefreshLock()) {
    const leaderOutcome = await waitForLeaderOutcome();
    if (leaderOutcome) {
      return leaderOutcome;
    }
  }

  const outcome = await runRefreshRequest();
  releaseRefreshLock(outcome);
  return outcome;
}

/** Single-flight внутри вкладки + координация между вкладками через localStorage-lock. */
async function tryRefreshSession(): Promise<RefreshOutcome> {
  if (!refreshRequestPromise) {
    refreshRequestPromise = coordinateRefresh().finally(() => {
      refreshRequestPromise = null;
    });
  }

  return refreshRequestPromise;
}

/**
 * Плановое продление сессии (heartbeat). Разлогинивает только при явном отказе сервера.
 */
export async function refreshSessionHeartbeat(): Promise<RefreshOutcome> {
  const outcome = await tryRefreshSession();
  if (outcome === 'dead') {
    onUnauthorized?.();
  }
  return outcome;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const originalRequest = error.config as (AxiosError['config'] & { _retry?: boolean }) | undefined;

    if (status === 401 && originalRequest && !originalRequest._retry && !shouldSkipRefresh(originalRequest)) {
      originalRequest._retry = true;
      const outcome = await tryRefreshSession();
      if (outcome === 'ok') {
        return apiClient.request(originalRequest);
      }
      if (outcome === 'transient') {
        // Сеть или 5xx на refresh: сессия жива, ошибку обрабатывает вызывающий код.
        return Promise.reject(error);
      }
    }

    if (status === 401) {
      onUnauthorized?.();
    } else if (status === 403) {
      onForbidden?.(getApiErrorMessage(error, 'Недостаточно прав для выполнения операции.'));
    }
    return Promise.reject(error);
  },
);

export interface DownloadResult {
  blob: Blob;
  filename: string;
}

function parseFilename(contentDisposition: string | undefined): string {
  if (!contentDisposition) {
    return 'download';
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const asciiMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return asciiMatch?.[1] ?? 'download';
}

export async function downloadBlob(
  url: string,
  params?: Record<string, string | number | boolean | undefined>,
  defaultFilename = 'download',
): Promise<DownloadResult> {
  const response = await apiClient.get<Blob>(url, {
    params,
    responseType: 'blob',
  });

  const filename = parseFilename(response.headers['content-disposition']) || defaultFilename;
  return { blob: response.data, filename };
}

export function triggerBrowserDownload(blob: Blob, filename: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(objectUrl);
}

export default apiClient;
