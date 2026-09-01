import type { ReactNode } from 'react';
import { ArrowDownRight, ArrowUpRight, Minus } from 'lucide-react';
import type { PerformanceReportVersionBlock } from '../../../api/performanceReportApi';
import { fmt, fmtSigned } from './reportFormat';
import { ReportCard, ReportCardContent } from './ui';

const METRIC_COLOR = 'var(--ep-report-metric)';

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
  return (
    <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
      {trendIcon}
      {fmtSigned(delta)} к плану
    </p>
  );
}

function MetricCard({
  title,
  value,
  subtitle,
  empty,
  testId,
  footer,
}: {
  title: string;
  value: ReactNode;
  subtitle?: string;
  empty: boolean;
  testId: string;
  footer?: ReactNode;
}) {
  return (
    <ReportCard
      className="border-l-4"
      style={{ borderLeftColor: empty ? 'var(--ep-report-border)' : METRIC_COLOR }}
    >
      <ReportCardContent className="flex flex-col gap-2">
        <p className="text-base font-semibold text-foreground md:text-lg">{title}</p>
        <p
          className="font-mono text-3xl font-semibold leading-none"
          style={{ color: empty ? undefined : METRIC_COLOR }}
          data-testid={testId}
        >
          {value}
        </p>
        {subtitle ? <p className="text-sm text-muted-foreground">{subtitle}</p> : null}
        {footer}
      </ReportCardContent>
    </ReportCard>
  );
}

export function SummaryCards({ plan, fact, accuracyPercent }: Props) {
  const planZ = plan?.weightedZScore ?? null;
  const factZ = fact?.weightedZScore ?? null;

  return (
    <div
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5"
      data-testid="report-summary-cards"
    >
      <MetricCard
        title="Заложенный Z"
        empty={!plan || planZ == null}
        value={plan ? fmtSigned(planZ) : '—'}
        subtitle={
          plan
            ? (plan.weightedZScoreInterpretation ?? 'Нет интерпретации')
            : 'Нет основной версии для оценки'
        }
        testId="report-plan-z"
      />
      <MetricCard
        title="Оценка Z"
        empty={!fact || factZ == null}
        value={fact ? fmtSigned(factZ) : '—'}
        subtitle={
          fact
            ? (fact.weightedZScoreInterpretation ?? 'Нет интерпретации')
            : 'Нет основной версии факта'
        }
        testId="report-fact-z"
        footer={<DeltaToPlan plan={plan} fact={fact} />}
      />
      <MetricCard
        title="Трудоёмкость — план"
        empty={!plan || plan.totalLaborHours == null}
        value={
          <>
            {fmt(plan?.totalLaborHours, 0)}
            <span className="ml-1 text-base font-normal text-muted-foreground">ч</span>
          </>
        }
        subtitle={plan ? `Версия: ${plan.versionName}` : 'Нет основной версии для оценки'}
        testId="report-labor-plan"
      />
      <MetricCard
        title="Трудоёмкость — факт"
        empty={!fact || fact.totalLaborHours == null}
        value={
          fact && fact.totalLaborHours != null ? (
            <>
              {fmt(fact.totalLaborHours, 0)}
              <span className="ml-1 text-base font-normal text-muted-foreground">ч</span>
            </>
          ) : (
            '—'
          )
        }
        subtitle={fact ? `Версия: ${fact.versionName}` : 'Нет основной версии факта'}
        testId="report-labor-fact"
      />
      <MetricCard
        title="Точность сравнения"
        empty={accuracyPercent == null}
        value={
          <>
            {fmt(accuracyPercent, 1)}
            <span className="ml-1 text-base font-normal text-muted-foreground">%</span>
          </>
        }
        subtitle="Средневзвешенная PD% по характеристикам"
        testId="report-accuracy"
      />
    </div>
  );
}
