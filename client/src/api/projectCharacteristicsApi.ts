import apiClient from '../services/apiClient';

export interface ProjectCharacteristicOption {
  id: number;
  name: string;
  valuePercent?: number | null;
}

export interface ProjectCharacteristicSelection {
  characteristicId: number;
  characteristicName: string;
  selectedOptionId?: number | null;
  selectedOptionName?: string | null;
  selectedValuePercent?: number | null;
  options: ProjectCharacteristicOption[];
}

export interface ProjectCharacteristicSelectionWrite {
  characteristicId: number;
  optionId: number;
}

export async function fetchProjectCharacteristics(
  projectId: number,
): Promise<ProjectCharacteristicSelection[]> {
  const { data } = await apiClient.get<ProjectCharacteristicSelection[]>(
    `/projects/${projectId}/characteristics`,
  );
  return data;
}

export async function saveProjectCharacteristics(
  projectId: number,
  selections: ProjectCharacteristicSelectionWrite[],
): Promise<void> {
  await apiClient.put(`/projects/${projectId}/characteristics`, {
    selections,
  });
}
