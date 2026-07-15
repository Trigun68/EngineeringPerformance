import { describe, expect, it } from 'vitest';
import { sanitizeProjectPayload } from '../../api/projectsApi';

describe('sanitizeProjectPayload', () => {
  it('converts empty workStart to null', () => {
    const payload = sanitizeProjectPayload({
      code: ' P1 ',
      name: ' Test ',
      workStart: '',
      workNumber: '',
    });
    expect(payload.workStart).toBeNull();
    expect(payload.code).toBe('P1');
    expect(payload.workNumber).toBeNull();
  });

  it('keeps valid workStart', () => {
    const payload = sanitizeProjectPayload({
      code: 'P1',
      name: 'Test',
      workStart: '2024-06-01',
    });
    expect(payload.workStart).toBe('2024-06-01');
  });
});
