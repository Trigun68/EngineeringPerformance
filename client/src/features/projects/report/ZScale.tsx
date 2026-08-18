import { fmtSigned, interpretationColor } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const MIN = -3;
const MAX = 3;

const ZONES = [
  { from: MIN, to: -1, label: 'Отлично', color: 'var(--ep-report-perf-excellent)' },
  { from: -1, to: -0.5, label: 'Хорошо', color: 'var(--ep-report-perf-good)' },
  { from: -0.5, to: 0.5, label: 'Удовлетв.', color: 'var(--ep-report-perf-satisfactory)' },
  { from: 0.5, to: 1, label: 'Внимание', color: 'var(--ep-report-perf-attention)' },
  { from: 1, to: MAX, label: 'Критично', color: 'var(--ep-report-perf-critical)' },
];

function pct(z: number): number {
  const clamped = Math.min(MAX, Math.max(MIN, z));
  return ((clamped - MIN) / (MAX - MIN)) * 100;
}

function dashPercentile(value: string | null | undefined): string {
  if (!value || value === 'Н/Д' || value === 'НД') return '—';
  return value;
}

type Marker = {
  zScore: number | null;
  label: string;
  side: 'top' | 'bottom';
};

type Props = {
  planZ: number | null;
  factZ: number | null;
  planPercentile: string;
  factPercentile: string;
  normSourcesUsed: number;
};

export function ZScale({
  planZ,
  factZ,
  planPercentile,
  factPercentile,
  normSourcesUsed,
}: Props) {
  // UX: план сверху, факт снизу.
  const markers: Marker[] = [
    { zScore: planZ, label: 'План', side: 'top' },
    { zScore: factZ, label: 'Факт', side: 'bottom' },
  ];

  const normsLabel =
    normSourcesUsed <= 0
      ? '—'
      : normSourcesUsed === 1
        ? '1 версия норм'
        : `${normSourcesUsed} версии норм`;

  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle className="text-base md:text-lg">Шкала интерпретации Z-оценки</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Диапазон −3…+3. Меньшая Z — выше производительность. Маркеры: план сверху, факт снизу.
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-6">
        <div className="pt-8 pb-10" data-testid="report-z-scale">
          <div className="relative">
            <div className="flex h-8 w-full overflow-hidden rounded-md">
              {ZONES.map((zone) => (
                <div
                  key={zone.label}
                  className="flex items-center justify-center"
                  style={{
                    width: `${((zone.to - zone.from) / (MAX - MIN)) * 100}%`,
                    backgroundColor: zone.color,
                  }}
                >
                  <span className="px-1 text-[11px] font-medium text-background">{zone.label}</span>
                </div>
              ))}
            </div>

            {markers.map((m) =>
              m.zScore == null ? null : (
                <div
                  key={m.label}
                  className={`absolute flex -translate-x-1/2 flex-col items-center ${
                    m.side === 'top' ? '-top-7' : 'top-8'
                  }`}
                  style={{ left: `${pct(m.zScore)}%` }}
                >
                  {m.side === 'top' ? (
                    <>
                      <span className="mb-0.5 text-[10px] font-medium text-muted-foreground">
                        {m.label}
                      </span>
                      <span
                        className="rounded-sm px-1.5 py-0.5 font-mono text-xs font-semibold text-background"
                        style={{ backgroundColor: interpretationColor(m.zScore) }}
                      >
                        {fmtSigned(m.zScore)}
                      </span>
                      <span
                        className="h-8 w-0.5"
                        style={{ backgroundColor: interpretationColor(m.zScore) }}
                      />
                    </>
                  ) : (
                    <>
                      <span
                        className="h-8 w-0.5"
                        style={{ backgroundColor: interpretationColor(m.zScore) }}
                      />
                      <span
                        className="rounded-sm px-1.5 py-0.5 font-mono text-xs font-semibold text-background"
                        style={{ backgroundColor: interpretationColor(m.zScore) }}
                      >
                        {fmtSigned(m.zScore)}
                      </span>
                      <span className="mt-0.5 text-[10px] font-medium text-muted-foreground">
                        {m.label}
                      </span>
                    </>
                  )}
                </div>
              ),
            )}

            <div className="mt-2 flex justify-between font-mono text-[11px] text-muted-foreground">
              {['−3', '−2', '−1', '0', '+1', '+2', '+3'].map((tick) => (
                <span key={tick}>{tick}</span>
              ))}
            </div>
          </div>
        </div>

        <dl className="grid grid-cols-2 gap-4 border-t border-border pt-4 sm:grid-cols-4">
          <div className="flex flex-col gap-1">
            <dt className="text-xs text-muted-foreground">Перцентиль плана</dt>
            <dd className="font-mono text-sm font-medium">{dashPercentile(planPercentile)}</dd>
          </div>
          <div className="flex flex-col gap-1">
            <dt className="text-xs text-muted-foreground">Перцентиль факта</dt>
            <dd className="font-mono text-sm font-medium">{dashPercentile(factPercentile)}</dd>
          </div>
          <div className="flex flex-col gap-1">
            <dt className="text-xs text-muted-foreground">Метод агрегации</dt>
            <dd className="text-sm font-medium">Σ(Z × часы) / Σ(часы)</dd>
          </div>
          <div className="flex flex-col gap-1">
            <dt className="text-xs text-muted-foreground">Источник норм</dt>
            <dd className="text-sm font-medium" data-testid="report-norm-sources">
              {normsLabel}
            </dd>
          </div>
        </dl>
      </ReportCardContent>
    </ReportCard>
  );
}
