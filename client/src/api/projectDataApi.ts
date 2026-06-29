import apiClient from '../services/apiClient';

export interface Paged<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
}

export interface ProjectPhysicalVolume {
  id: number;
  projectId: number;
  projectAnalysisVersionId: number;
  physicalVolumeId: number;
  quantity: number;
  facilityCode?: string | null;
  facilityName?: string | null;
  workName?: string | null;
  laborHoursPerUnit?: number | null;
}

export interface ProjectLaborIntensity {
  id: number;
  projectId: number;
  projectAnalysisVersionId: number;
  laborIntensityId?: number | null;
  plannedHours?: number | null;
  actualHours?: number | null;
}

export interface ProjectEquipment {
  id: number;
  projectId: number;
  projectAnalysisVersionId: number;
  equipmentId: number;
  quantity: number;
  quantityInWork?: number | null;
}

export async function fetchPhysicalVolumes(
  projectId: number,
  versionId: number,
  query: PagedQuery = {},
): Promise<Paged<ProjectPhysicalVolume>> {
  const { data } = await apiClient.get<Paged<ProjectPhysicalVolume>>(
    `/projects/${projectId}/versions/${versionId}/physical-volumes`,
    { params: { page: query.page ?? 1, pageSize: query.pageSize ?? 100, sort: query.sort } },
  );
  return data;
}

export async function fetchLaborIntensities(
  projectId: number,
  versionId: number,
  query: PagedQuery = {},
): Promise<Paged<ProjectLaborIntensity>> {
  const { data } = await apiClient.get<Paged<ProjectLaborIntensity>>(
    `/projects/${projectId}/versions/${versionId}/labor-intensities`,
    { params: { page: query.page ?? 1, pageSize: query.pageSize ?? 100, sort: query.sort } },
  );
  return data;
}

export async function fetchEquipment(
  projectId: number,
  versionId: number,
  query: PagedQuery = {},
): Promise<Paged<ProjectEquipment>> {
  const { data } = await apiClient.get<Paged<ProjectEquipment>>(
    `/projects/${projectId}/versions/${versionId}/equipment`,
    { params: { page: query.page ?? 1, pageSize: query.pageSize ?? 100, sort: query.sort } },
  );
  return data;
}
