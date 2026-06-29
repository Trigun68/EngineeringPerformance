import axios, { type AxiosError, type AxiosInstance } from 'axios';
import { getApiErrorMessage } from './apiError';

type UnauthorizedHandler = () => void;
type ForbiddenHandler = (message?: string) => void;

let onUnauthorized: UnauthorizedHandler | null = null;
let onForbidden: ForbiddenHandler | null = null;
let refreshRequestPromise: Promise<boolean> | null = null;

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
const SKIP_REFRESH_HEADER = 'X-Skip-Auth-Refresh';

export function setUnauthorizedHandler(handler: UnauthorizedHandler): void {
  onUnauthorized = handler;
}

export function setForbiddenHandler(handler: ForbiddenHandler): void {
  onForbidden = handler;
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

async function tryRefreshSession(): Promise<boolean> {
  if (!refreshRequestPromise) {
    refreshRequestPromise = axios
      .post(`${API_BASE_URL}/auth/refresh`, null, {
        withCredentials: true,
        headers: {
          [SKIP_REFRESH_HEADER]: '1',
        },
      })
      .then(() => true)
      .catch(() => false)
      .finally(() => {
        refreshRequestPromise = null;
      });
  }

  return refreshRequestPromise;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const originalRequest = error.config as (AxiosError['config'] & { _retry?: boolean }) | undefined;

    if (status === 401 && originalRequest && !originalRequest._retry && !shouldSkipRefresh(originalRequest)) {
      originalRequest._retry = true;
      const refreshed = await tryRefreshSession();
      if (refreshed) {
        return apiClient.request(originalRequest);
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
