import apiClient from '../services/apiClient';

export interface ProjectVersionPlanningParameter {
  id: number;
  iicWorkDirectionId: number;
  parentId: number | null;
  sortOrder: number;
  code: string;
  name: string;
  unitName: string | null;
  zPessimistic: number;
  zBase: number;
  zOptimistic: number;
  zActual: number | null;
  /** Вес LI Base для средневзвешенного rollup (с сервера). */
  laborHoursWeight?: number;
}

export interface PlanningParameterWriteItem {
  iicWorkDirectionId: number;
  zPessimistic: number;
  zBase: number;
  zOptimistic: number;
}

export async function fetchPlanningParameters(
  projectId: number,
  versionId: number,
): Promise<ProjectVersionPlanningParameter[]> {
  const { data } = await apiClient.get<ProjectVersionPlanningParameter[]>(
    `/projects/${projectId}/versions/${versionId}/planning-parameters`,
  );
  return data;
}

export async function savePlanningParameters(
  projectId: number,
  versionId: number,
  items: PlanningParameterWriteItem[],
): Promise<void> {
  await apiClient.put(`/projects/${projectId}/versions/${versionId}/planning-parameters`, { items });
}
