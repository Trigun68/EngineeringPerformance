import { useMemo } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import {
  fetchProductivityTrend,
  type ProductivityVolumeMode,
} from '../../../api/productivityTrendApi';
import {
  fetchProductivityIndexTrend,
  type ProductivityIndexPoint,
} from '../../../api/productivityIndexTrendApi';
import { fetchZTrends } from '../../../api/zTrendApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import type { GroupTrendLike, TrendPointLike, TrendVersionRow } from '../shared/trendDetailCalc';

function toTrendPoint(p: ProductivityIndexPoint): TrendPointLike {
  return { date: p.date, value: p.index, weight: p.weightHours };
}

function toVersionRow(p: ProductivityIndexPoint): TrendVersionRow {
  return {
    projectCode: p.projectCode,
    projectName: p.projectName,
    versionName: p.versionName,
    date: p.date,
    value: p.index,
  };
}

export function useProductivityTrendData(
  dateFrom: string,
  dateTo: string,
  volumeMode: ProductivityVolumeMode,
) {
  const trendQuery = useQuery({
    queryKey: queryKeys.analytics.productivityTrend(dateFrom, dateTo, volumeMode),
    queryFn: ({ signal }) =>
      fetchProductivityTrend(
        { periodStart: dateFrom || null, periodEnd: dateTo || null, volumeMode },
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const indexQuery = useQuery({
    queryKey: queryKeys.analytics.productivityIndex(dateFrom, dateTo),
    queryFn: ({ signal }) =>
      fetchProductivityIndexTrend(
        { periodStart: dateFrom || null, periodEnd: dateTo || null },
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const hoursQuery = useQuery({
    queryKey: queryKeys.analytics.zTrends(dateFrom, dateTo, 'basisHours'),
    queryFn: ({ signal }) =>
      fetchZTrends(
        {
          periodStart: dateFrom || null,
          periodEnd: dateTo || null,
          weightMode: 'basisHours',
        },
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const result = trendQuery.data ?? null;
  const loading = trendQuery.isFetching;
  const error =
    trendQuery.isError && !isRequestCanceled(trendQuery.error)
      ? getApiErrorMessage(trendQuery.error, 'Не удалось загрузить тренд производительности')
      : null;

  const indexResult = indexQuery.data ?? null;
  const indexLoading = indexQuery.isFetching;
  const indexError =
    indexQuery.isError && !isRequestCanceled(indexQuery.error)
      ? getApiErrorMessage(indexQuery.error, 'Не удалось загрузить индекс производительности')
      : null;
  const hours = hoursQuery.data ?? null;

  const indexPoints = useMemo(
    () => ({
      plan: (indexResult?.overall.plan ?? []).map(toTrendPoint),
      fact: (indexResult?.overall.fact ?? []).map(toTrendPoint),
    }),
    [indexResult],
  );
  const indexVersions = useMemo(
    () => ({
      plan: (indexResult?.overall.plan ?? []).map(toVersionRow),
      fact: (indexResult?.overall.fact ?? []).map(toVersionRow),
    }),
    [indexResult],
  );
  const indexWaterfallGroups = useMemo<GroupTrendLike[]>(
    () =>
      (indexResult?.byGroup ?? []).map((g) => ({
        groupId: g.groupId,
        groupName: g.groupName,
        points: g.fact.map(toTrendPoint),
      })),
    [indexResult],
  );
  const hasIndexPoints = indexPoints.plan.length > 0 || indexPoints.fact.length > 0;

  return {
    result,
    loading,
    error,
    indexResult,
    indexLoading,
    indexError,
    hours,
    indexPoints,
    indexVersions,
    indexWaterfallGroups,
    hasIndexPoints,
  };
}
