export interface ReferenceRouteConfig {
  path: string;
  resource: string;
  title: string;
  permission: string;
  isHierarchical?: boolean;
  showExcel?: boolean;
  /** formOrder порога для поля «Родитель» в модальном окне */
  parentFormOrder?: number;
}

/** Порядок и названия вкладок как в WPF ReferencesPageViewModel */
export const REFERENCE_ROUTES: ReferenceRouteConfig[] = [
  {
    path: 'physical-volumes-kfo',
    resource: 'physical-volumes-kfo',
    title: 'Номенклатура физических объёмов – КФО',
    permission: 'PhysicalVolumes',
    isHierarchical: true,
    parentFormOrder: 65,
  },
  {
    path: 'physical-volumes-ber',
    resource: 'physical-volumes-ber',
    title: 'Номенклатура физических объёмов – БЕР',
    permission: 'PhysicalVolumes',
    isHierarchical: true,
    parentFormOrder: 65,
  },
  {
    path: 'equipment',
    resource: 'equipment',
    title: 'Номенклатура оборудования',
    permission: 'Equipment',
    isHierarchical: true,
    parentFormOrder: 45,
  },
  {
    path: 'labor-intensities',
    resource: 'labor-intensities',
    title: 'Трудоёмкость',
    permission: 'LaborIntensities',
    isHierarchical: true,
    parentFormOrder: 35,
  },
  {
    path: 'iic-work-directions',
    resource: 'iic-work-directions',
    title: 'Группы физ. объёмов (IIC)',
    permission: 'IicWorkDirections',
    isHierarchical: true,
    parentFormOrder: 45,
  },
  {
    path: 'iic-norms',
    resource: 'iic-norms',
    title: 'Нормы физобъёмов (IIC)',
    permission: 'IicWorkDirectionNorms',
    isHierarchical: true,
  },
  {
    path: 'units-of-measurement',
    resource: 'units-of-measurement',
    title: 'Единицы измерения',
    permission: 'UnitsOfMeasurement',
  },
  {
    path: 'project-characteristics',
    resource: 'project-characteristics',
    title: 'Характеристики проекта',
    permission: 'ProjectCharacteristics',
    showExcel: false,
  },
];

/** Справочники номенклатуры физ. объёмов — группировка дерева по коду. */
export function isPhysicalVolumeCatalogResource(resource: string): boolean {
  return resource === 'physical-volumes-kfo' || resource === 'physical-volumes-ber';
}
