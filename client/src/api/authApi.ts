import apiClient from '../services/apiClient';

export interface WindowsIdentityHint {
  windowsUsername: string | null;
  displayName: string | null;
  source: 'proxy' | 'development' | 'none';
}

export async function fetchWindowsIdentityHint(): Promise<WindowsIdentityHint> {
  const { data } = await apiClient.get<{
    windowsUsername?: string | null;
    WindowsUsername?: string | null;
    displayName?: string | null;
    DisplayName?: string | null;
    source?: string;
    Source?: string;
  }>('/auth/windows-identity');

  const sourceRaw = (data.source ?? data.Source ?? 'none').toLowerCase();
  const source: WindowsIdentityHint['source'] =
    sourceRaw === 'proxy' ? 'proxy' : sourceRaw === 'development' ? 'development' : 'none';

  return {
    windowsUsername: data.windowsUsername ?? data.WindowsUsername ?? null,
    displayName: data.displayName ?? data.DisplayName ?? null,
    source,
  };
}

export async function refreshAuthSession(): Promise<void> {
  await apiClient.post('/auth/refresh', null, {
    headers: {
      'X-Skip-Auth-Refresh': '1',
    },
  });
}
