import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export interface UserActionLogEntry {
  id: number;
  userId: number;
  userName: string;
  timestamp: string;
  actionType: string;
  entityType: string | null;
  entityId: number | null;
  description: string | null;
  ipOrHost: string | null;
  isArchived?: boolean;
}

export interface UserActionLogQuery {
  from?: string;
  to?: string;
  userId?: number;
  actionType?: string;
  includeArchived?: boolean;
  skip?: number;
  take?: number;
}

export interface UserActionLogRestoreResult {
  totalRows: number;
  importedCount: number;
  skippedDuplicates: number;
  skippedOrphans: number;
  skippedInvalid: number;
  errors: string[];
}

export async function fetchUserActionLogs(query: UserActionLogQuery = {}): Promise<UserActionLogEntry[]> {
  const { data } = await apiClient.get<UserActionLogEntry[]>('/audit/user-actions', { params: query });
  return data;
}

export async function archiveUserActionLogs(ids: number[]): Promise<void> {
  await apiClient.post('/audit/user-actions/archive', { ids });
}

export async function restoreUserActionLogs(file: File): Promise<UserActionLogRestoreResult> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<UserActionLogRestoreResult>(
    '/audit/user-actions/restore',
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}

export async function exportUserActionLogsExcel(query: UserActionLogQuery = {}): Promise<void> {
  const { blob, filename } = await downloadBlob(
    '/audit/user-actions/export.xlsx',
    query as Record<string, string | number | boolean | undefined>,
    'user-action-log.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}
