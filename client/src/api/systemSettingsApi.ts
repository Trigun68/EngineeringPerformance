import apiClient from '../services/apiClient';

export interface SystemSettings {
  sessionExpirationMinutes: number;
}

export async function fetchSystemSettings(): Promise<SystemSettings> {
  const { data } = await apiClient.get<SystemSettings>('/admin/system-settings');
  return data;
}

export async function updateSystemSettings(settings: SystemSettings): Promise<void> {
  await apiClient.put('/admin/system-settings', settings);
}
