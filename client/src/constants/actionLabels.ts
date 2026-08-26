/**
 * Единый реестр подписей кнопок (экспорт, импорт, удаление данных версии).
 * Текст на экране, в aria-label, контекстном меню и e2e — только отсюда.
 */
export const ACTION_LABELS = {
  exportExcel: 'Выгрузить Excel',
  exportPdf: 'Выгрузить PDF',
  exportExcelSelected: 'Выгрузить Excel (выделенное)',
  exportExcelSummary: 'Выгрузить Excel (сводная)',
  exportCsv: 'Выгрузить CSV',
  exportExcelHealth: 'Выгрузить Excel (состояние)',

  importExcel: 'Импорт Excel',
  importPhysicalVolumes: 'Импорт ФО',
  importEquipment: 'Импорт оборудования',
  importLabor: 'Импорт трудоёмкости',

  deletePhysicalVolumes: 'Удалить ФО',
  deleteEquipment: 'Удалить оборудование',
  deleteLabor: 'Удалить трудоёмкость',

  refresh: 'Обновить',
  selectFile: 'Выбрать файл',
} as const;

export type ActionLabelKey = keyof typeof ACTION_LABELS;
