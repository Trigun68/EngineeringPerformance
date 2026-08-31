/** Отображаемые названия объектов матрицы прав (зеркало WPF AccessControlConstants). */
const OBJECT_NAME_RU: Record<string, string> = {
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
};

export function toRussianObjectName(objectName: string): string {
  return OBJECT_NAME_RU[objectName] ?? objectName;
}
