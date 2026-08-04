import type { PerformanceReportDto } from '../../../api/performanceReportApi';
import { fmt } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

type Props = {
  accuracy: PerformanceReportDto['accuracy'];
};

export function AccuracyPanel({ accuracy }: Props) {
  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle>Точность сравнения (характеристики)</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Средняя PD% показывает сопоставимость выборки ИИС с объектом. Чем ниже — тем осторожнее
          трактовать Z.
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-4">
        {accuracy.items.length === 0 ? (
          <p className="text-sm text-muted-foreground">Характеристики не заданы.</p>
        ) : (
          accuracy.items.map((item) => (
            <div key={item.characteristicName} className="flex flex-col gap-1.5">
              <div className="flex items-baseline justify-between gap-4">
                <span className="text-sm font-medium">{item.characteristicName}</span>
                <span className="font-mono text-sm">{fmt(item.valuePercent, 0)}%</span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-sm bg-muted">
                <span
                  className="block h-full rounded-sm bg-primary"
                  style={{ width: `${Math.min(Math.max(item.valuePercent ?? 0, 0), 100)}%` }}
                />
              </div>
              <span className="text-xs text-muted-foreground">{item.optionName ?? '—'}</span>
            </div>
          ))
        )}
        <div className="flex items-baseline justify-between gap-4 border-t border-border pt-4">
          <span className="text-sm font-semibold">Итоговая точность</span>
          <span className="font-mono text-lg font-semibold">{fmt(accuracy.averagePercent, 1)}%</span>
        </div>
      </ReportCardContent>
    </ReportCard>
  );
}
