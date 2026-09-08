import { describe, expect, it } from 'vitest';
import { getApiErrorMessage } from '../apiError';

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
