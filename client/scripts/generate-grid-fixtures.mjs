/**
 * Генерация фикстур для Vitest/Playwright (9 гридов + справочники).
 * Запуск: npm run fixtures:grid
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const fixturesDir = join(__dirname, '../tests/fixtures');
mkdirSync(fixturesDir, { recursive: true });

const dictionaries = {
  physicalVolumeTypes: [{ id: 1, name: 'Тип 1', code: 'NT1' }],
  physicalVolumes: [],
  equipments: [],
  laborIntensities: [],
  iicWorkDirections: [
    { id: 1, name: 'Направление А', code: 'DIR-A', parentId: null },
    { id: 2, name: 'Направление Б', code: 'DIR-B', parentId: 1 },
  ],
  iicWorkDirectionNorms: [],
  unitsOfMeasurement: [
    { id: 1, name: 'шт' },
    { id: 2, name: 'м' },
  ],
  projectCharacteristics: [],
};

const gridData = {
  forecast: {
    summary: [
      {
        workDirectionId: 1,
        parentId: null,
        name: 'Направление А',
        unitName: 'шт',
        normProductivity: 10,
        normEstimate: 2.3,
        normStandardDeviation: 0.5,
        plannedVolume: 100,
        optimisticHours: 950,
        baseHours: 1000,
        pessimisticHours: 1050,
        range: '950 – 1050',
        hierarchyPath: 'Направление А',
      },
    ],
    facility: [
      { facilityCode: 'F1', facilityName: 'Объект 1', laborHours: 500, zScore: null, interpretation: '' },
    ],
    detail: [
      {
        rowId: 1,
        facilityCode: 'F1',
        facilityName: 'Объект 1',
        documentSet: 'DS-1',
        catalogCode: '01-01-001-01',
        catalogName: 'Работа 1',
        workId: 'W1',
        workName: 'Монтаж',
        iicGroupName: 'DIR-A Направление А',
        iicUnitName: 'шт',
        quantity: 10,
        baseHours: 100,
        optimisticHours: 95,
        pessimisticHours: 105,
        factLaborHours: null,
        factProductivity: null,
        zScore: null,
        interpretation: '',
        isEquipment: false,
      },
    ],
  },
  fact: {
    performance: [
      {
        workDirectionId: 1,
        workDirectionName: 'Направление А',
        workDirectionCode: 'DIR-A',
        parentId: null,
        level: 0,
        normUnitName: 'шт',
        factPhysicalVolume: 80,
        factLaborHours: 800,
        factProductivity: 10,
        factEstimate: 2.3,
        normProductivity: 10,
        normEstimate: 2.3,
        normStdDev: 0.5,
        zScore: 0.1,
        interpretation: 'Normal',
        subtreeHours: 800,
      },
    ],
    facility: [
      { facilityCode: 'F1', facilityName: 'Объект 1', laborHours: 400, zScore: 0.2, interpretation: 'Норма' },
    ],
    detail: [
      {
        rowId: 2,
        facilityCode: 'F1',
        facilityName: 'Объект 1',
        documentSet: 'DS-1',
        catalogCode: '01-01-001-01',
        catalogName: 'Работа 1',
        workId: 'W1',
        workName: 'Монтаж',
        iicGroupName: 'DIR-A Направление А',
        iicUnitName: 'шт',
        quantity: 8,
        baseHours: null,
        optimisticHours: null,
        pessimisticHours: null,
        factLaborHours: 80,
        factProductivity: 10,
        zScore: 0.1,
        interpretation: 'Норма',
        isEquipment: false,
      },
    ],
  },
  compare: {
    summary: [
      {
        workDirectionId: 1,
        parentId: null,
        name: 'Направление А',
        zScoreA: 0.1,
        zScoreB: 0.3,
        zDelta: 0.2,
        interpretationA: 'Норма',
        interpretationB: 'Норма',
      },
    ],
    facility: [
      {
        facilityCode: 'F1',
        facilityName: 'Объект 1',
        factLaborHoursA: 400,
        factLaborHoursB: 420,
        hoursDelta: 20,
        zScoreA: 0.2,
        zScoreB: 0.25,
        zDelta: 0.05,
        interpretationA: 'Норма',
        interpretationB: 'Норма',
      },
    ],
    detail: [
      {
        facilityCode: 'F1',
        facilityName: 'Объект 1',
        documentSet: 'DS-1',
        pvCode: '01-01-001-01',
        pvName: 'Работа 1',
        workId: 'W1',
        workName: 'Монтаж',
        quantityA: 8,
        quantityB: 9,
        quantityDelta: 1,
        factLaborHoursA: 80,
        factLaborHoursB: 90,
        hoursDelta: 10,
        factProductivityA: 10,
        factProductivityB: 10,
        productivityDelta: 0,
        iicGroupName: 'DIR-A',
        iicUnitName: 'шт',
        presence: 'Both',
      },
    ],
  },
};

writeFileSync(join(fixturesDir, 'dictionaries.json'), JSON.stringify(dictionaries, null, 2));
writeFileSync(join(fixturesDir, 'grid-data.json'), JSON.stringify(gridData, null, 2));
console.log('Fixtures written to', fixturesDir);
