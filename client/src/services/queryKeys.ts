/**
 * Фабрики ключей React Query.
 * Формы должны совпадать с уже существующими queryKey, иначе кэш «протухает».
 *
 * fact/forecast/compare: projectId — второй элемент, чтобы root(projectId)
 * был префиксом pageData/detail/report и сбрасывал кэш удалённых версий.
 * projects.all() без аргумента = ['projects','all'] — префикс для showArchived.
 */
export const queryKeys = {
  projects: {
    // Список проектов сейчас без флага архива — третий элемент не добавляем,
    // иначе ключ ['projects','all', undefined] не совпадёт с ['projects','all'].
    all: (showArchived?: boolean) =>
      showArchived === undefined
        ? (['projects', 'all'] as const)
        : (['projects', 'all', showArchived] as const),
  },
  project: (projectId: number | string | undefined) => ['project', projectId] as const,
  projectVersions: (projectId: number | string | undefined) =>
    ['projectVersions', projectId] as const,
  projectCharacteristics: (projectId: number | string | undefined) =>
    ['projectCharacteristics', projectId] as const,
  projectPermissions: (projectId: number | string | undefined) =>
    ['projectPermissions', projectId] as const,
  dictionaries: {
    all: () => ['dictionaries', 'all'] as const,
  },
  registers: {
    // Нормы IIC и стоимость работ — массивы совпадают с уже существующими queryKey.
    iicNormsRoot: ['registers', 'iic-norms'] as const,
    iicNorms: (asOfDate: string | undefined) => ['registers', 'iic-norms', asOfDate] as const,
    iicNormsHistory: (workDirectionId: number | null) =>
      ['registers', 'iic-norms', 'history', workDirectionId] as const,
    workCosts: {
      // Префикс для invalidateQueries по всем запросам стоимости работ.
      root: ['registers', 'work-costs'] as const,
      asOf: (asOfDate: string | undefined) =>
        ['registers', 'work-costs', 'as-of', asOfDate] as const,
      periods: ['registers', 'work-costs', 'periods'] as const,
      period: (from: string | undefined) => ['registers', 'work-costs', 'period', from] as const,
    },
  },
  fact: {
    root: (projectId: number | string | undefined) => ['fact', projectId] as const,
    pageData: (projectId: number | undefined, versionId: number | undefined) =>
      ['fact', projectId, 'page-data', versionId] as const,
    detail: (projectId: number | undefined, versionId: number | undefined, subKey?: string) =>
      ['fact', projectId, 'detail', versionId, subKey] as const,
  },
  forecast: {
    root: (projectId: number | string | undefined) => ['forecast', projectId] as const,
    pageData: (projectId: number | undefined, versionId: number | undefined) =>
      ['forecast', projectId, 'page-data', versionId] as const,
    detail: (projectId: number | undefined, versionId: number | undefined, subKey?: string) =>
      ['forecast', projectId, 'detail', versionId, subKey] as const,
    report: (projectId: number | undefined, versionId: number | undefined) =>
      ['forecast', projectId, 'report', versionId] as const,
    planningParameters: (projectId: number | undefined, versionId: number | undefined) =>
      ['forecast', projectId, 'planning-parameters', versionId] as const,
  },
  analytics: {
    root: ['analytics'] as const,
    projectTrend: (projectId: number | undefined) =>
      ['analytics', 'project-trend', projectId] as const,
    groupAnalysis: (projectId: number | undefined) =>
      ['analytics', 'group-analysis', projectId] as const,
    hoursTrend: (projectId: number | undefined, dateFrom: string, dateTo: string) =>
      ['analytics', 'hours-trend', projectId, dateFrom, dateTo] as const,
    hoursTrendDetail: (projectId: number | undefined, versionId: number | undefined) =>
      ['analytics', 'hours-trend-detail', projectId, versionId] as const,
    zTrends: (dateFrom: string, dateTo: string, weightMode: string) =>
      ['analytics', 'z-trends', dateFrom, dateTo, weightMode] as const,
    productivityTrend: (dateFrom: string, dateTo: string, volumeMode: string) =>
      ['analytics', 'productivity-trend', dateFrom, dateTo, volumeMode] as const,
    productivityIndex: (dateFrom: string, dateTo: string) =>
      ['analytics', 'productivity-index', dateFrom, dateTo] as const,
    normRegistry: (filterKey: string) =>
      ['analytics', 'norm-registry', filterKey] as const,
    factNorm: (dateFrom: string, dateTo: string, scopeKey: string) =>
      ['analytics', 'fact-norm', dateFrom, dateTo, scopeKey] as const,
  },
  compare: {
    root: (projectId: number | string | undefined) => ['compare', projectId] as const,
    pageData: (
      projectId: number | undefined,
      versionAId: number | undefined,
      versionBId: number | undefined,
    ) => ['compare', projectId, 'page-data', versionAId, versionBId] as const,
    detail: (
      projectId: number | undefined,
      versionAId: number | undefined,
      versionBId: number | undefined,
      subKey?: string,
    ) => ['compare', projectId, 'detail', versionAId, versionBId, subKey] as const,
  },
  directory: {
    root: (resource: string) => ['directory-page', resource] as const,
    page: (
      resource: string,
      showArchived: boolean,
      selectedProjectId: string | number | null,
    ) => ['directory-page', resource, showArchived, selectedProjectId] as const,
  },
  reports: {
    performance: (projectId: number | undefined) =>
      ['reports', 'performance', projectId] as const,
    versionAnalysis: (projectId: number | undefined, versionId: number | undefined) =>
      ['reports', 'version-analysis', projectId, versionId] as const,
  },
};
