import { AlertTriangle, CircleAlert, TrendingDown } from 'lucide-react';
import type { PerformanceReportDto } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportBadge, ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const KIND_META: Record<string, { icon: typeof AlertTriangle; color: string; label: string }> = {
  risk: { icon: AlertTriangle, color: 'var(--ep-report-perf-critical)', label: 'Риск' },
  attention: { icon: CircleAlert, color: 'var(--ep-report-perf-attention)', label: 'Контроль' },
  strength: { icon: TrendingDown, color: 'var(--ep-report-perf-excellent)', label: 'Сильная сторона' },
};

type Props = {
  insights: PerformanceReportDto['insights'];
  totalHours: number | null;
};

export function InsightsPanel({ insights, totalHours }: Props) {
  const hours = totalHours ?? 0;

  return (
    <div data-testid="report-insights">
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle className="text-base md:text-lg">Выводы и рекомендации</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Top-3 по влиянию |Z| × часы поддерева. Объём в контексте — факт, иначе план.
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-6">
        {insights.items.length === 0 ? (
          <p className="text-sm text-muted-foreground">Недостаточно данных для выводов.</p>
        ) : (
          <ul className="flex flex-col gap-4">
            {insights.items.map((insight) => {
              const meta = KIND_META[insight.kind] ?? KIND_META.attention;
              const Icon = meta.icon;
              return (
                <li key={`${insight.kind}-${insight.title}`} className="flex gap-3">
                  <span
                    className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-muted"
                    style={{ color: meta.color }}
                  >
                    <Icon className="size-4" aria-hidden />
                  </span>
                  <div className="flex flex-col gap-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-sm font-medium">{insight.title}</span>
                      <ReportBadge style={{ color: meta.color, borderColor: 'currentColor' }}>
                        {meta.label}
                      </ReportBadge>
                    </div>
                    <p className="text-sm leading-relaxed text-muted-foreground">{insight.detail}</p>
                  </div>
                </li>
              );
            })}
          </ul>
        )}

        <div className="grid grid-cols-1 gap-6 border-t border-border pt-4 md:grid-cols-2">
          <TopList
            title="Top-3 перерасход"
            items={insights.topNegative}
            totalHours={hours}
            empty="Нет направлений с Z > 0"
          />
          <TopList
            title="Top-3 опережение"
            items={insights.topPositive}
            totalHours={hours}
            empty="Нет направлений с Z < 0"
          />
        </div>
      </ReportCardContent>
    </ReportCard>
    </div>
  );
}

function TopList({
  title,
  items,
  totalHours,
  empty,
}: {
  title: string;
  items: PerformanceReportDto['insights']['topNegative'];
  totalHours: number;
  empty: string;
}) {
  return (
    <div className="flex flex-col gap-3">
      <div>
        <h3 className="text-sm font-semibold">{title}</h3>
        <p className="text-xs text-muted-foreground">Влияние = |Z| × часы поддерева</p>
      </div>
      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">{empty}</p>
      ) : (
        items.map((row) => {
          const share = totalHours > 0 ? (row.subtreeHours / totalHours) * 100 : 0;
          const color = interpretationColor(row.zScore);
          return (
            <div key={row.workDirectionId} className="flex flex-col gap-1">
              <div className="flex items-baseline justify-between gap-2">
                <span className="truncate text-sm font-medium" title={row.name}>
                  {row.name}
                </span>
                <span className="font-mono text-xs font-semibold" style={{ color }}>
                  {fmtSigned(row.zScore)}
                </span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-sm bg-muted">
                <span
                  className="block h-full rounded-sm"
                  style={{ width: `${Math.min(share, 100)}%`, backgroundColor: color }}
                />
              </div>
              <span className="font-mono text-[11px] text-muted-foreground">
                {fmt(row.subtreeHours, 0)} ч · влияние {fmt(row.influenceScore, 1)}
                {row.physicalVolume != null ? ` · объём ${fmt(row.physicalVolume)}` : ''}
              </span>
            </div>
          );
        })
      )}
    </div>
  );
}
