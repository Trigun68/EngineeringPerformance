import { useCallback, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../../api/projectsPageApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import type { ProjectsPageData } from '../types';

export type FactPageSubTab = 'fact-total' | 'fact-facility' | 'fact-detail';

export interface UseFactPageDataParams {
  projectId: number;
  factVersionId: number | null;
  subTab: FactPageSubTab;
  canRead: boolean;
}

export interface UseFactPageDataResult {
  pageData: ProjectsPageData | null;
  isFetching: boolean;
  detailLoading: boolean;
  pageDataError: string | null;
  detailError: string | null;
  reloadPageData: () => Promise<void>;
}

/** Page-data + ленивый detail факта и invalidate для диалогов. */
export function useFactPageData({
  projectId,
  factVersionId,
  subTab,
  canRead,
}: UseFactPageDataParams): UseFactPageDataResult {
  const queryClient = useQueryClient();

  const pageDataQuery = useQuery({
    queryKey: queryKeys.fact.pageData(projectId, factVersionId ?? undefined),
    queryFn: ({ signal }) => fetchProjectsPageData(projectId, factVersionId!, undefined, signal),
    enabled: canRead && factVersionId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.fact.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке fact-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.fact.detail(projectId, factVersionId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchProjectsPageDetails(projectId, factVersionId!, undefined, signal),
    enabled:
      canRead
      && factVersionId != null
      && subTab === 'fact-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const pageData = useMemo((): ProjectsPageData | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data?.factDetail;
    if (lazyDetail === undefined) return base;
    return { ...base, fact: { ...base.fact, detail: lazyDetail } };
  }, [detailQuery.data, pageDataQuery.data]);

  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить данные оценки факта.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальные данные оценки факта.')
      : null;

  const reloadPageData = useCallback(async () => {
    if (!factVersionId || !canRead) return;
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: queryKeys.fact.pageData(projectId, factVersionId),
      }),
      queryClient.invalidateQueries({
        queryKey: queryKeys.fact.detail(projectId, factVersionId),
      }),
    ]);
  }, [canRead, factVersionId, projectId, queryClient]);

  return {
    pageData,
    isFetching: pageDataQuery.isFetching,
    detailLoading: detailQuery.isFetching,
    pageDataError,
    detailError,
    reloadPageData,
  };
}
