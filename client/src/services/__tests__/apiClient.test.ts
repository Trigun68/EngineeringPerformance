import { beforeEach, describe, expect, it, vi } from 'vitest';

type ErrorInterceptor = (error: unknown) => Promise<unknown>;

const postMock = vi.fn();
const requestMock = vi.fn();
let errorInterceptor: ErrorInterceptor;

vi.mock('axios', () => {
  const instance = {
    interceptors: {
      response: {
        use: (_onSuccess: unknown, onError: ErrorInterceptor) => {
          errorInterceptor = onError;
        },
      },
    },
    request: requestMock,
  };

  const axiosMock = {
    create: () => instance,
    post: postMock,
    isAxiosError: (error: unknown) => Boolean((error as { isAxiosError?: boolean })?.isAxiosError),
  };

  return { default: axiosMock };
});

const REFRESH_LOCK_KEY = 'engineering-performance-auth-refresh';

function unauthorizedRequest(url = '/projects') {
  return {
    isAxiosError: true,
    response: { status: 401 },
    config: { url, headers: {} },
  };
}

async function loadApiClient() {
  vi.resetModules();
  return import('../apiClient');
}

beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
});

describe('apiClient — обновление сессии', () => {
  it('повторяет исходный запрос после успешного refresh', async () => {
    const { setUnauthorizedHandler } = await loadApiClient();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);

    postMock.mockResolvedValue({ status: 204 });
    requestMock.mockResolvedValue({ data: 'retried' });

    await expect(errorInterceptor(unauthorizedRequest())).resolves.toEqual({ data: 'retried' });

    expect(postMock).toHaveBeenCalledTimes(1);
    expect(requestMock).toHaveBeenCalledTimes(1);
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('разлогинивает только при явном отказе сервера на refresh', async () => {
    const { setUnauthorizedHandler } = await loadApiClient();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);

    postMock.mockRejectedValue({ isAxiosError: true, response: { status: 401 } });

    await expect(errorInterceptor(unauthorizedRequest())).rejects.toBeDefined();

    expect(onUnauthorized).toHaveBeenCalledTimes(1);
    expect(requestMock).not.toHaveBeenCalled();
  });

  it.each([
    ['обрыв сети', { isAxiosError: true }],
    ['5xx на backend', { isAxiosError: true, response: { status: 503 } }],
  ])('сохраняет сессию при временном сбое refresh: %s', async (_name, refreshError) => {
    const { setUnauthorizedHandler } = await loadApiClient();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);

    postMock.mockRejectedValue(refreshError);

    await expect(errorInterceptor(unauthorizedRequest())).rejects.toBeDefined();

    expect(onUnauthorized).not.toHaveBeenCalled();
    expect(requestMock).not.toHaveBeenCalled();
  });

  it('объединяет параллельные 401 в один запрос refresh', async () => {
    await loadApiClient();

    postMock.mockResolvedValue({ status: 204 });
    requestMock.mockResolvedValue({ data: 'retried' });

    await Promise.all([
      errorInterceptor(unauthorizedRequest('/projects')),
      errorInterceptor(unauthorizedRequest('/dictionaries/all')),
    ]);

    expect(postMock).toHaveBeenCalledTimes(1);
    expect(requestMock).toHaveBeenCalledTimes(2);
  });

  it('ждёт результат вкладки-лидера вместо повторной ротации токена', async () => {
    const { refreshSessionHeartbeat } = await loadApiClient();

    localStorage.setItem(
      REFRESH_LOCK_KEY,
      JSON.stringify({ startedAt: Date.now(), outcome: null }),
    );
    setTimeout(() => {
      localStorage.setItem(
        REFRESH_LOCK_KEY,
        JSON.stringify({ startedAt: Date.now(), outcome: 'ok' }),
      );
    }, 200);

    await expect(refreshSessionHeartbeat()).resolves.toBe('ok');
    expect(postMock).not.toHaveBeenCalled();
  });

  it('heartbeat не разлогинивает при временном сбое', async () => {
    const { refreshSessionHeartbeat, setUnauthorizedHandler } = await loadApiClient();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);

    postMock.mockRejectedValue({ isAxiosError: true, response: { status: 502 } });

    await expect(refreshSessionHeartbeat()).resolves.toBe('transient');
    expect(onUnauthorized).not.toHaveBeenCalled();
  });
});
