/** Отображаемые названия объектов матрицы прав (зеркало WPF AccessControlConstants). */
const OBJECT_NAME_RU = {
  Users: 'Пользователи',
  Projects: 'Проекты',
  PhysicalVolumes: 'Номенклатура физических объёмов',
  Equipment: 'Номенклатура оборудования',
  PhysicalVolumeTypes: 'Виды справочников физ. объёмов',
  IicWorkDirections: 'Группы физ. объёмов (IIC)',
  LaborIntensities: 'Трудоёмкость',
  ProjectAnalysisVersions: 'Версии анализа проекта',
  IicWorkDirectionNorms: 'Нормы групп физ. объёмов (IIC)',
  EngineeringWorkCosts: 'Стоимость работ',
  DocSets: 'Комплекты документации',
  UnitsOfMeasurement: 'Единицы измерения',
  ProjectCharacteristics: 'Характеристики проекта',
  ProjectPhysicalVolumes: 'Физ. объёмы по проекту',
  ProjectEquipments: 'Оборудование по проекту',
  ProjectLaborIntensities: 'Трудоёмкость по проекту',
  ProjectDataUpload: 'Загрузка данных проекта',
  Assessment: 'Оценка',
  Planning: 'Планирование',
  VersionComparison: 'Сравнение версий',
  Reports: 'Отчёты',
  GroupAnalysis: 'Анализ по группам',
  GroupTrend: 'Тренд по группам',
  References: 'Справочники',
  DataPage: 'Данные',
  HelpPage: 'Справка',
  SettingsPage: 'Настройки',
  Dashboard: 'Главная',
  UserActionLog: 'Журнал действий',
  /** Только SuperAdmin (мониторинг, admin API); в AccessRights не сеется — зеркало PermissionMatrixHierarchy.cs. */
  Administration: 'Администрирование',
} as const;

/** Имена объектов прав — те же строки, что backend PermissionType / WPF AccessControlConstants. */
export type PermissionObjectName = keyof typeof OBJECT_NAME_RU;

export const PERMISSION_OBJECTS = Object.keys(OBJECT_NAME_RU) as readonly PermissionObjectName[];

/** Типизированные константы для `usePermission` / `objectName` (H1). */
export const OBJECT_NAME = Object.fromEntries(
  PERMISSION_OBJECTS.map((key) => [key, key]),
) as { readonly [K in PermissionObjectName]: K };

export function toRussianObjectName(objectName: string): string {
  return (OBJECT_NAME_RU as Record<string, string>)[objectName] ?? objectName;
}
