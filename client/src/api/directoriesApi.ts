import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { buildExportFileName } from '../utils/directoryExport';

export interface DirectoryEntry {
  id: number;
  name: string;
  parentId: number | null;
  sortOrder: number;
  isArchived: boolean;
  createdAt: string;
  modifiedAt: string | null;
  properties: Record<string, unknown>;
}

export interface DirectoryWriteRequest {
  name: string;
  parentId?: number | null;
  sortOrder?: number;
  properties?: Record<string, unknown>;
}

export interface DirectoryTreeNode {
  id: number;
  name: string;
  parentId: number | null;
  sortOrder: number;
  isArchived: boolean;
  properties: Record<string, unknown>;
  children: DirectoryTreeNode[];
}

export interface DirectoryTreeRow extends DirectoryEntry {
  hierarchy: string[];
}

/** API properties могут приходить в PascalCase — приводим к camelCase для UI. */
export function normalizeDirectoryProperties(
  properties: Record<string, unknown>,
): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(properties)) {
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      continue;
    }
    if (Array.isArray(value)) {
      continue;
    }
    const camel = key.length > 0 ? key.charAt(0).toLowerCase() + key.slice(1) : key;
    result[camel] = value;
  }
  return result;
}

function normalizeEntry(entry: DirectoryEntry): DirectoryEntry {
  return {
    ...entry,
    properties: normalizeDirectoryProperties(entry.properties),
  };
}

function normalizeTreeNode(node: DirectoryTreeNode): DirectoryTreeNode {
  return {
    ...node,
    properties: normalizeDirectoryProperties(node.properties),
    children: node.children.map(normalizeTreeNode),
  };
}

export function flattenDirectoryTree(
  nodes: DirectoryTreeNode[],
  parentPath: string[] = [],
  options?: { hierarchyByCode?: boolean },
): DirectoryTreeRow[] {
  const rows: DirectoryTreeRow[] = [];
  for (const node of nodes) {
    const code = node.properties.code ?? node.properties.Code;
    const segment = options?.hierarchyByCode && code != null && String(code).length > 0
      ? String(code)
      : String(node.id);
    const hierarchy = [...parentPath, segment];
    rows.push({
      id: node.id,
      name: node.name,
      parentId: node.parentId,
      sortOrder: node.sortOrder,
      isArchived: node.isArchived,
      createdAt: '',
      modifiedAt: null,
      properties: normalizeDirectoryProperties(node.properties),
      hierarchy,
    });
    if (node.children.length > 0) {
      rows.push(...flattenDirectoryTree(node.children, hierarchy, options));
    }
  }
  return rows;
}

export async function fetchDirectoryTree(
  resource: string,
  includeArchived = false,
  projectId?: number | null,
): Promise<DirectoryTreeNode[]> {
  const { data } = await apiClient.get<DirectoryTreeNode[]>(`/directories/${resource}/tree`, {
    params: {
      includeArchived,
      ...(projectId != null ? { projectId } : {}),
    },
  });
  return data.map(normalizeTreeNode);
}

export interface PagedDirectoryResponse {
  items: DirectoryEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function fetchDirectory(
  resource: string,
  includeArchived = false,
  page = 1,
  pageSize = 100,
  projectId?: number | null,
): Promise<PagedDirectoryResponse> {
  const { data } = await apiClient.get<PagedDirectoryResponse>(`/directories/${resource}`, {
    params: {
      includeArchived,
      page,
      pageSize,
      ...(projectId != null ? { projectId } : {}),
    },
  });
  return {
    ...data,
    items: data.items.map(normalizeEntry),
  };
}

export async function fetchDirectoryEntry(resource: string, id: number): Promise<DirectoryEntry> {
  const { data } = await apiClient.get<DirectoryEntry>(`/directories/${resource}/${id}`);
  return normalizeEntry(data);
}

export async function createDirectoryEntry(resource: string, body: DirectoryWriteRequest): Promise<DirectoryEntry> {
  const { data } = await apiClient.post<DirectoryEntry>(`/directories/${resource}`, body);
  return data;
}

export async function updateDirectoryEntry(resource: string, id: number, body: DirectoryWriteRequest): Promise<DirectoryEntry> {
  const { data } = await apiClient.put<DirectoryEntry>(`/directories/${resource}/${id}`, body);
  return data;
}

export async function archiveDirectoryEntry(resource: string, id: number): Promise<void> {
  await apiClient.post(`/directories/${resource}/${id}/archive`);
}

export async function unarchiveDirectoryEntry(resource: string, id: number): Promise<void> {
  await apiClient.post(`/directories/${resource}/${id}/unarchive`);
}

export async function deleteDirectoryEntry(resource: string, id: number): Promise<void> {
  await apiClient.delete(`/directories/${resource}/${id}`);
}

export interface CharacteristicOption {
  id: number;
  characteristicId: number;
  name: string;
  valuePercent?: number | null;
  sortOrder: number;
}

export async function fetchCharacteristicOptions(characteristicId: number): Promise<CharacteristicOption[]> {
  const { data } = await apiClient.get<CharacteristicOption[]>(`/characteristics/${characteristicId}/options`);
  return data;
}

export async function createCharacteristicOption(
  characteristicId: number,
  body: { name: string; valuePercent?: number | null; sortOrder?: number },
): Promise<CharacteristicOption> {
  const { data } = await apiClient.post<CharacteristicOption>(`/characteristics/${characteristicId}/options`, body);
  return data;
}

export async function updateCharacteristicOption(
  characteristicId: number,
  optionId: number,
  body: { name: string; valuePercent?: number | null; sortOrder?: number },
): Promise<CharacteristicOption> {
  const { data } = await apiClient.put<CharacteristicOption>(
    `/characteristics/${characteristicId}/options/${optionId}`,
    body,
  );
  return data;
}

export async function deleteCharacteristicOption(characteristicId: number, optionId: number): Promise<void> {
  await apiClient.delete(`/characteristics/${characteristicId}/options/${optionId}`);
}

export async function exportDirectoryExcel(
  resource: string,
  tabName: string,
  headers: string[],
  rows: string[][],
): Promise<void> {
  const { data } = await apiClient.post<ArrayBuffer>(
    `/directories/${resource}/export`,
    { tabName, headers, rows },
    { responseType: 'arraybuffer' },
  );

  const filename = buildExportFileName(tabName);
  const blob = new Blob([data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  triggerBrowserDownload(blob, filename);
}

export async function exportDirectoryExcelLegacy(resource: string): Promise<void> {
  const { blob, filename } = await downloadBlob(`/directories/${resource}/export.xlsx`, undefined, `${resource}.xlsx`);
  triggerBrowserDownload(blob, filename);
}
