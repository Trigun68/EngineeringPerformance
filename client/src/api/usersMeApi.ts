import apiClient from '../services/apiClient';

export interface CurrentUserProfile {
  id: number;
  login: string;
  displayName: string;
  windowsAccount: string | null;
  role: string;
  isActive: boolean;
}

interface CurrentUserProfileResponse {
  id?: number;
  Id?: number;
  login?: string;
  Login?: string;
  displayName?: string;
  DisplayName?: string;
  windowsAccount?: string | null;
  WindowsAccount?: string | null;
  role?: string;
  Role?: string;
  isActive?: boolean;
  IsActive?: boolean;
}

function normalizeProfile(data: CurrentUserProfileResponse): CurrentUserProfile {
  return {
    id: data.id ?? data.Id ?? 0,
    login: data.login ?? data.Login ?? '',
    displayName: data.displayName ?? data.DisplayName ?? '',
    windowsAccount: data.windowsAccount ?? data.WindowsAccount ?? null,
    role: data.role ?? data.Role ?? '',
    isActive: data.isActive ?? data.IsActive ?? true,
  };
}

export async function fetchMe(): Promise<CurrentUserProfile> {
  const { data } = await apiClient.get<CurrentUserProfileResponse>('/users/me');
  return normalizeProfile(data);
}

export async function changeOwnPassword(
  oldPassword: string,
  newPassword: string,
  confirmPassword: string,
): Promise<void> {
  await apiClient.post('/users/me/change-password', {
    oldPassword,
    newPassword,
    confirmPassword,
  });
}

export async function changeUserPassword(userId: number, newPassword: string): Promise<void> {
  await apiClient.post(`/users/${userId}/change-password`, { newPassword });
}
