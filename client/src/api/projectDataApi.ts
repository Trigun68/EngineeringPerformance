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

export interface ProjectPhysicalVolumeWriteRequest {
  physicalVolumeId: number;
  quantity: number;
  facilityCode?: string | null;
  facilityName?: string | null;
  workName?: string | null;
  laborHoursPerUnit?: number | null;
}

export interface ProjectEquipmentWriteRequest {
  equipmentId: number;
  quantity: number;
  quantityInWork?: number | null;
}

async function findInPaged<T>(
  fetchPage: (query: PagedQuery) => Promise<Paged<T>>,
  predicate: (item: T) => boolean,
): Promise<T | null> {
  let page = 1;
  const pageSize = 500;
  while (true) {
    const result = await fetchPage({ page, pageSize });
    const found = result.items.find(predicate);
    if (found) return found;
    if (page * pageSize >= result.totalCount) return null;
    page += 1;
  }
}

export async function fetchPhysicalVolumeById(
  projectId: number,
  versionId: number,
  id: number,
): Promise<ProjectPhysicalVolume | null> {
  return findInPaged(
    (query) => fetchPhysicalVolumes(projectId, versionId, query),
    (item) => item.id === id,
  );
}

export async function fetchEquipmentById(
  projectId: number,
  versionId: number,
  id: number,
): Promise<ProjectEquipment | null> {
  return findInPaged(
    (query) => fetchEquipment(projectId, versionId, query),
    (item) => item.id === id,
  );
}

export async function createPhysicalVolume(
  projectId: number,
  versionId: number,
  request: ProjectPhysicalVolumeWriteRequest,
): Promise<ProjectPhysicalVolume> {
  const { data } = await apiClient.post<ProjectPhysicalVolume>(
    `/projects/${projectId}/versions/${versionId}/physical-volumes`,
    request,
  );
  return data;
}

export async function createEquipment(
  projectId: number,
  versionId: number,
  request: ProjectEquipmentWriteRequest,
): Promise<ProjectEquipment> {
  const { data } = await apiClient.post<ProjectEquipment>(
    `/projects/${projectId}/versions/${versionId}/equipment`,
    request,
  );
  return data;
}

export async function updatePhysicalVolume(
  projectId: number,
  versionId: number,
  id: number,
  request: ProjectPhysicalVolumeWriteRequest,
): Promise<ProjectPhysicalVolume> {
  const { data } = await apiClient.put<ProjectPhysicalVolume>(
    `/projects/${projectId}/versions/${versionId}/physical-volumes/${id}`,
    request,
  );
  return data;
}

export async function deletePhysicalVolume(
  projectId: number,
  versionId: number,
  id: number,
): Promise<void> {
  await apiClient.delete(`/projects/${projectId}/versions/${versionId}/physical-volumes/${id}`);
}

export async function updateEquipment(
  projectId: number,
  versionId: number,
  id: number,
  request: ProjectEquipmentWriteRequest,
): Promise<ProjectEquipment> {
  const { data } = await apiClient.put<ProjectEquipment>(
    `/projects/${projectId}/versions/${versionId}/equipment/${id}`,
    request,
  );
  return data;
}

export async function deleteEquipment(
  projectId: number,
  versionId: number,
  id: number,
): Promise<void> {
  await apiClient.delete(`/projects/${projectId}/versions/${versionId}/equipment/${id}`);
}
