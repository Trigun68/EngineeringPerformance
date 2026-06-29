import apiClient from '../services/apiClient';

export async function getUserSetting(key: string): Promise<string | null> {
  try {
    const { data } = await apiClient.get<{ key: string; value: string }>(`/users/me/settings/${encodeURIComponent(key)}`);
    return data.value;
  } catch {
    return null;
  }
}

export async function putUserSetting(key: string, value: string): Promise<void> {
  await apiClient.put(`/users/me/settings/${encodeURIComponent(key)}`, { value });
}
