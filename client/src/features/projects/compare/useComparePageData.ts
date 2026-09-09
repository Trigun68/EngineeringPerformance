import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  fetchVersionCompare,
  fetchVersionCompareDetail,
  type VersionCompareResult,
} from '../../../api/analyticsApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import type { CompareSubTab } from './useCompareExport';

export interface UseComparePageDataParams {
  projectId: number;
  versionAId: number | null;
  versionBId: number | null;
  subTab: CompareSubTab;
  canRead: boolean;
}

export interface UseComparePageDataResult {
  compareData: VersionCompareResult | null;
  isFetching: boolean;
  detailLoading: boolean;
  pageDataError: string | null;
  detailError: string | null;
}

/** Page-data + ленивый detail сравнения версий. */
export function useComparePageData({
  projectId,
  versionAId,
  versionBId,
  subTab,
  canRead,
}: UseComparePageDataParams): UseComparePageDataResult {
  // Перезагрузка — invalidateQueries по queryKeys.compare.pageData / .detail
  // (префикс detail без subKey сбрасывает ленивый кэш). Смена A/B сама меняет ключ.
  const pageDataQuery = useQuery({
    queryKey: queryKeys.compare.pageData(projectId, versionAId ?? undefined, versionBId ?? undefined),
    queryFn: ({ signal }) => fetchVersionCompare(projectId, versionAId!, versionBId!, signal),
    enabled: canRead && versionAId != null && versionBId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке compare-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.compare.detail(projectId, versionAId ?? undefined, versionBId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchVersionCompareDetail(projectId, versionAId!, versionBId!, signal),
    enabled:
      canRead
      && versionAId != null
      && versionBId != null
      && subTab === 'compare-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const compareData = useMemo((): VersionCompareResult | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data;
    if (lazyDetail === undefined) return base;
    return { ...base, detail: lazyDetail };
  }, [detailQuery.data, pageDataQuery.data]);

  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить сравнение версий.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальное сравнение версий.')
      : null;

  return {
    compareData,
    isFetching: pageDataQuery.isFetching,
    detailLoading: detailQuery.isFetching,
    pageDataError,
    detailError,
  };
}
