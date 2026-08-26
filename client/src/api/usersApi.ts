import apiClient from '../services/apiClient';
import type { AppUser, RoleOption, UserFormData } from '../types/user';

export interface UserLookup {
  id: number;
  username: string;
  displayName: string;
  roleName: string;
}

export async function fetchUsers(includeArchived = false): Promise<AppUser[]> {
  const { data } = await apiClient.get<AppUser[]>('/users', {
    params: { includeArchived },
  });
  return data;
}

export async function fetchRoles(): Promise<RoleOption[]> {
  const { data } = await apiClient.get<RoleOption[]>('/users/roles');
  return data;
}

export async function fetchUsersLookup(): Promise<UserLookup[]> {
  const { data } = await apiClient.get<UserLookup[]>('/users/lookup');
  return data;
}

export async function createUser(form: UserFormData): Promise<AppUser> {
  const { data } = await apiClient.post<AppUser>('/users', {
    username: form.username,
    displayName: form.displayName,
    roleId: form.roleId,
    password: form.password || null,
    windowsUsername: form.windowsUsername || null,
    isActive: form.isActive,
    mustChangePassword: form.mustChangePassword,
  });
  return data;
}

export async function updateUser(id: number, form: UserFormData): Promise<AppUser> {
  const { data } = await apiClient.put<AppUser>(`/users/${id}`, {
    username: form.username,
    displayName: form.displayName,
    roleId: form.roleId,
    password: form.password || null,
    windowsUsername: form.windowsUsername || null,
    isActive: form.isActive,
  });
  return data;
}

export async function deleteUser(id: number): Promise<void> {
  await apiClient.delete(`/users/${id}`);
}

export async function archiveUser(id: number): Promise<void> {
  await apiClient.post(`/users/${id}/archive`);
}

export async function unarchiveUser(id: number): Promise<void> {
  await apiClient.post(`/users/${id}/unarchive`);
}

export async function unlockUser(id: number): Promise<void> {
  await apiClient.post(`/users/${id}/unlock`);
}

export async function resetUserMfa(id: number): Promise<void> {
  await apiClient.post(`/users/${id}/reset-mfa`);
}

export async function resetUserPassword(id: number): Promise<string> {
  const { data } = await apiClient.post<{ temporaryPassword: string }>(`/users/${id}/reset-password`);
  return data.temporaryPassword;
}
