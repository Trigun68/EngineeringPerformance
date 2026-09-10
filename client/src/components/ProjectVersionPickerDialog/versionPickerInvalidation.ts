import type { QueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../services/queryKeys';

/** Сброс списка версий и pageData факта/прогноза/сравнения после CRUD версии. */
export async function invalidateAfterVersionMutation(
  queryClient: QueryClient,
  projectId: number,
): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: queryKeys.projectVersions(projectId) }),
    queryClient.invalidateQueries({ queryKey: queryKeys.fact.root(projectId) }),
    queryClient.invalidateQueries({ queryKey: queryKeys.forecast.root(projectId) }),
    queryClient.invalidateQueries({ queryKey: queryKeys.compare.root(projectId) }),
  ]);
}
