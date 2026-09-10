import { describe, expect, it, vi } from 'vitest';
import { QueryClient } from '@tanstack/react-query';
import { invalidateAfterVersionMutation } from '../versionPickerInvalidation';
import { queryKeys } from '../../../services/queryKeys';

describe('invalidateAfterVersionMutation', () => {
  it('invalidates versions, fact, forecast and compare for the project', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');
    await invalidateAfterVersionMutation(queryClient, 7);
    expect(spy).toHaveBeenCalledTimes(4);
    expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.projectVersions(7) });
    expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.fact.root(7) });
    expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.forecast.root(7) });
    expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.compare.root(7) });
  });
});
