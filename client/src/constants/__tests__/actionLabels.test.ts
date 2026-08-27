import { describe, expect, it } from 'vitest';
import { ACTION_LABELS } from '../actionLabels';

describe('ACTION_LABELS', () => {
  it('keeps canonical export captions', () => {
    expect(ACTION_LABELS.exportExcel).toBe('Выгрузить Excel');
    expect(ACTION_LABELS.exportPdf).toBe('Выгрузить PDF');
    expect(ACTION_LABELS.exportExcelSelected).toBe('Выгрузить Excel (выделенное)');
    expect(ACTION_LABELS.exportExcelSummary).toBe('Выгрузить Excel (сводная)');
    expect(ACTION_LABELS.exportCsv).toBe('Выгрузить CSV');
    expect(ACTION_LABELS.exportExcelHealth).toBe('Выгрузить Excel (состояние)');
    expect(ACTION_LABELS.refresh).toBe('Обновить');
    expect(ACTION_LABELS.selectFile).toBe('Выбрать файл');
  });

  it('does not use the old «Экспорт …» / abbreviated import wording', () => {
    const values = Object.values(ACTION_LABELS);
    expect(values.some((label) => label.startsWith('Экспорт'))).toBe(false);
    expect(values).not.toContain('Экспорт всех excel');
    expect(values).not.toContain('Экспорт выделенных excel');
    expect(values).not.toContain('Выгрузить в Excel');
    expect(values).not.toContain('Импорт оборуд.');
    expect(values).not.toContain('Импорт труд.');
  });
});
