import type { ReactNode } from 'react';
import { ArrowDownRight, ArrowUpRight, Minus } from 'lucide-react';
import type { PerformanceReportVersionBlock } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportCard, ReportCardContent } from './ui';

type Props = {
  plan: PerformanceReportVersionBlock | null;
  fact: PerformanceReportVersionBlock | null;
  accuracyPercent: number | null;
};

function DeltaToPlan({
  plan,
  fact,
}: {
  plan: PerformanceReportVersionBlock | null;
  fact: PerformanceReportVersionBlock | null;
}) {
  if (!plan || !fact || plan.weightedZScore == null || fact.weightedZScore == null) return null;
  const delta = fact.weightedZScore - plan.weightedZScore;
  const trendIcon =
    Math.abs(delta) < 0.005 ? (
      <Minus className="size-3.5" aria-hidden />
    ) : delta < 0 ? (
      <ArrowDownRight className="size-3.5" aria-hidden />
    ) : (
      <ArrowUpRight className="size-3.5" aria-hidden />
    );
  const trendColor =
    Math.abs(delta) < 0.005
      ? 'var(--ep-report-muted-foreground)'
      : delta < 0
        ? 'var(--ep-report-perf-excellent)'
        : 'var(--ep-report-perf-critical)';
  return (
    <p className="flex items-center gap-1.5 text-xs" style={{ color: trendColor }}>
      {trendIcon}
      {fmtSigned(delta)} к плану ({plan.versionName})
    </p>
  );
}

function ZCard({
  title,
  block,
  emptyHint,
  footer,
  testId,
}: {
  title: string;
  block: PerformanceReportVersionBlock | null;
  emptyHint: string;
  footer?: ReactNode;
  testId: string;
}) {
  const z = block?.weightedZScore ?? null;
  const color = interpretationColor(z);
  return (
    <ReportCard className="border-l-4" style={{ borderLeftColor: color }}>
      <ReportCardContent className="flex flex-col gap-2">
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</p>
        <p
          className="font-mono text-3xl font-semibold leading-none"
          style={{ color }}
          data-testid={testId}
        >
          {block ? fmtSigned(z) : '—'}
        </p>
        <p className="text-sm" style={{ color: block ? color : undefined }}>
          {block
            ? (block.weightedZScoreInterpretation ?? 'Нет интерпретации')
            : emptyHint}
        </p>
        {block ? (
          <p className="text-xs text-muted-foreground">
            Перцентиль {block.weightedZScorePercentile || '—'} · {block.versionName}
          </p>
        ) : null}
        {footer}
      </ReportCardContent>
    </ReportCard>
  );
}

export function SummaryCards({ plan, fact, accuracyPercent }: Props) {
  return (
    <div
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4"
      data-testid="report-summary-cards"
    >
      <ZCard
        title="Z-оценка плана"
        block={plan}
        emptyHint="Версия плана не выбрана"
        testId="report-plan-z"
      />
      <ZCard
        title="Z-оценка факта"
        block={fact}
        emptyHint="Нет версии факта"
        testId="report-fact-z"
        footer={<DeltaToPlan plan={plan} fact={fact} />}
      />

      <ReportCard>
        <ReportCardContent className="flex flex-col gap-2">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Трудоёмкость план / факт
          </p>
          <p className="font-mono text-2xl font-semibold leading-none" data-testid="report-labor-hours">
            <span>{fmt(plan?.totalLaborHours, 0)}</span>
            <span className="mx-1 text-base font-normal text-muted-foreground">/</span>
            <span>{fmt(fact?.totalLaborHours, 0)}</span>
            <span className="ml-1 text-base font-normal text-muted-foreground">ч</span>
          </p>
          <p className="text-sm text-muted-foreground">
            Не отнесено (факт): {fmt(fact?.unallocatedLaborHours ?? plan?.unallocatedLaborHours, 0)} ч
          </p>
        </ReportCardContent>
      </ReportCard>

      <ReportCard>
        <ReportCardContent className="flex flex-col gap-2">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Точность сравнения
          </p>
          <p className="font-mono text-3xl font-semibold leading-none" data-testid="report-accuracy">
            {fmt(accuracyPercent, 1)}
            <span className="ml-1 text-base font-normal text-muted-foreground">%</span>
          </p>
          <p className="text-sm text-muted-foreground">Средняя PD% по характеристикам проекта</p>
        </ReportCardContent>
      </ReportCard>
    </div>
  );
}
