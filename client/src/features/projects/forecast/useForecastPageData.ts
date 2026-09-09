import { useCallback, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../../api/projectsPageApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import type { ProjectsPageData } from '../types';
import type { ForecastToolbarSubTab } from './ForecastToolbar';

export interface UseForecastPageDataParams {
  projectId: number;
  planVersionId: number | null;
  subTab: ForecastToolbarSubTab;
  canRead: boolean;
}

export interface UseForecastPageDataResult {
  pageData: ProjectsPageData | null;
  isFetching: boolean;
  detailLoading: boolean;
  pageDataError: string | null;
  detailError: string | null;
  reloadPageData: () => Promise<void>;
}

/** Page-data + ленивый detail прогноза и invalidate для диалогов. */
export function useForecastPageData({
  projectId,
  planVersionId,
  subTab,
  canRead,
}: UseForecastPageDataParams): UseForecastPageDataResult {
  const queryClient = useQueryClient();

  const pageDataQuery = useQuery({
    queryKey: queryKeys.forecast.pageData(projectId, planVersionId ?? undefined),
    queryFn: ({ signal }) => fetchProjectsPageData(projectId, planVersionId!, undefined, signal),
    enabled: canRead && planVersionId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.forecast.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке forecast-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.forecast.detail(projectId, planVersionId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchProjectsPageDetails(projectId, planVersionId!, undefined, signal),
    enabled:
      canRead
      && planVersionId != null
      && subTab === 'forecast-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const pageData = useMemo((): ProjectsPageData | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data?.forecastDetail;
    if (lazyDetail === undefined) return base;
    return { ...base, forecast: { ...base.forecast, detail: lazyDetail } };
  }, [detailQuery.data, pageDataQuery.data]);

  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить данные прогнозирования.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальные данные прогнозирования.')
      : null;

  const reloadPageData = useCallback(async () => {
    if (!planVersionId || !canRead) return;
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: queryKeys.forecast.pageData(projectId, planVersionId),
      }),
      queryClient.invalidateQueries({
        queryKey: queryKeys.forecast.detail(projectId, planVersionId),
      }),
    ]);
  }, [canRead, planVersionId, projectId, queryClient]);

  return {
    pageData,
    isFetching: pageDataQuery.isFetching,
    detailLoading: detailQuery.isFetching,
    pageDataError,
    detailError,
    reloadPageData,
  };
}
