import apiClient from '../services/apiClient';

export async function getUserPreferences(): Promise<Record<string, string>> {
  const { data } = await apiClient.get<Record<string, string>>('/users/me/preferences');
  return data ?? {};
}

export async function putUserPreference(key: string, value: string): Promise<void> {
  await apiClient.put('/users/me/preferences', { key, value });
}

export async function putUserPreferences(preferences: Record<string, string>): Promise<void> {
  await apiClient.put('/users/me/preferences', { preferences });
}
