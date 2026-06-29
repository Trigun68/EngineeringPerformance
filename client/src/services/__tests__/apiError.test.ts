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

  it('maps 403 to permission message when body empty', () => {
    const message = getApiErrorMessage({ response: { status: 403, data: {} } });
    expect(message).toBe('Недостаточно прав для выполнения операции.');
  });
});
