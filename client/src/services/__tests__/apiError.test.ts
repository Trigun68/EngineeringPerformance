import { describe, expect, it } from 'vitest';
import { formatApiError, getApiErrorHint, getApiErrorMessage } from '../apiError';

describe('getApiErrorMessage', () => {
  it('prefers detail over title from ProblemDetails', () => {
    const message = getApiErrorMessage({
      response: {
        status: 400,
        data: { title: 'Bad Request', detail: 'Project code already exists.' },
      },
    });
    expect(message).toBe('Project code already exists.');
  });

  it('prefers field errors over generic validation detail', () => {
    const message = getApiErrorMessage({
      response: {
        status: 400,
        data: {
          title: 'Validation failed.',
          detail: 'One or more validation errors occurred.',
          errors: { body: ['Укажите хотя бы одно значение нормы (выработку, оценку, σ или формулу).'] },
        },
      },
    });
    expect(message).toBe(
      'Укажите хотя бы одно значение нормы (выработку, оценку, σ или формулу).',
    );
  });

  it('maps 403 to permission message when body empty', () => {
    const message = getApiErrorMessage({ response: { status: 403, data: {} } });
    expect(message).toBe('Недостаточно прав для выполнения операции.');
  });

  it('maps 410 to gone message when body empty', () => {
    const message = getApiErrorMessage({ response: { status: 410, data: {} } });
    expect(message).toBe(
      'Запись больше не доступна (удалена или архивирована). Обновите список.',
    );
  });

  it('maps 413 to upload size message when body empty', () => {
    const message = getApiErrorMessage({ response: { status: 413, data: {} } });
    expect(message).toBe('Файл слишком большой. Максимальный размер — 10 МБ.');
  });

  it('maps 409 to concurrency message when body empty', () => {
    const message = getApiErrorMessage({ response: { status: 409, data: {} } });
    expect(message).toBe('Версия изменена другим пользователем. Обновите данные и повторите.');
  });
});

describe('getApiErrorHint', () => {
  it('returns the 401 session hint', () => {
    expect(getApiErrorHint(401)).toBe('Сессия истекла — войдите снова.');
  });

  it('returns the 403 permission hint', () => {
    expect(getApiErrorHint(403)).toBe('Недостаточно прав для этого действия.');
  });

  it('returns the 410 tunnel/DNS hint', () => {
    expect(getApiErrorHint(410)).toBe(
      'Запрос ушёл не в Engineering Performance (проверьте Cloudflare Tunnel / DNS web на edge).',
    );
  });

  it('returns null for other statuses', () => {
    expect(getApiErrorHint(404)).toBeNull();
    expect(getApiErrorHint(500)).toBeNull();
    expect(getApiErrorHint(undefined)).toBeNull();
  });
});

describe('formatApiError', () => {
  it('appends the 401 hint after the API message', () => {
    expect(formatApiError({ response: { status: 401, data: {} } }, 'Не удалось загрузить.')).toBe(
      'Требуется авторизация. Сессия истекла — войдите снова.',
    );
  });

  it('appends the 403 hint after the API message', () => {
    expect(formatApiError({ response: { status: 403, data: {} } }, 'Не удалось загрузить.')).toBe(
      'Недостаточно прав для выполнения операции. Недостаточно прав для этого действия.',
    );
  });

  it('appends the 410 hint after the gone message', () => {
    expect(formatApiError({ response: { status: 410, data: {} } }, 'Не удалось загрузить.')).toBe(
      'Запись больше не доступна (удалена или архивирована). Обновите список. Запрос ушёл не в Engineering Performance (проверьте Cloudflare Tunnel / DNS web на edge).',
    );
  });

  it('uses fallback without a hint when status is not special', () => {
    expect(formatApiError({ response: { status: 500, data: {} } }, 'Не удалось загрузить справочник.')).toBe(
      'Не удалось загрузить справочник.',
    );
  });

  it('uses fallback when the error has no HTTP response', () => {
    expect(formatApiError(new Error('network'), 'Не удалось загрузить справочник.')).toBe(
      'Не удалось загрузить справочник.',
    );
  });
});
