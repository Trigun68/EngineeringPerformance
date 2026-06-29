import apiClient from '../services/apiClient';

export interface AccessRight {
  id: number;
  roleId: number;
  roleName: string;
  objectName: string;
  accessLevel: number;
}

export async function fetchAccessRights(): Promise<AccessRight[]> {
  const { data } = await apiClient.get<AccessRight[]>('/access-rights');
  return data;
}

export async function updateAccessRight(id: number, accessLevel: number): Promise<void> {
  await apiClient.put(`/access-rights/${id}`, { accessLevel });
}
