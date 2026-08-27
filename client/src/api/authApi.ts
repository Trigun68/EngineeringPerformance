import apiClient from '../services/apiClient';
import type { LoginResponse, MfaSetupResponse, MfaStatus } from '../types/auth';

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

export async function verifyMfa(code: string, mfaSessionToken: string): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/mfa/verify', { code, mfaSessionToken });
  return data;
}

export async function beginMfaSetup(): Promise<MfaSetupResponse> {
  const { data } = await apiClient.post<MfaSetupResponse>('/auth/mfa/setup');
  return data;
}

export async function completeMfaSetup(code: string, secret: string): Promise<void> {
  await apiClient.post('/auth/mfa/verify-setup', { code, secret });
}

export async function getMfaStatus(): Promise<MfaStatus> {
  const { data } = await apiClient.get<MfaStatus>('/auth/mfa/status');
  return data;
}
