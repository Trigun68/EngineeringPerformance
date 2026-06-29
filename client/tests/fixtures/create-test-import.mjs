/**
 * Генерация client/tests/fixtures/test_import.xlsx для E2E (3 строки оборудования).
 * Запуск: node tests/fixtures/create-test-import.mjs
 */
import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { deflateRawSync } from 'node:zlib';

const __dirname = dirname(fileURLToPath(import.meta.url));
const outPath = join(__dirname, 'test_import.xlsx');

const headers = [
  'Id',
  '№ позиции в основном списке',
  'FBS (Code and Title of Facility)',
  'Комплект документов',
  'Диаграмма No / PFD No / P&ID No/ Datasheet No',
  'Наименование краткое',
  'Наименование по каталогу',
  'Тип/Марка оборудования',
  'ID по каталогу',
  'TAG',
  'ID обор. полный',
  'Стандарт на поставку',
  'ОЛ',
  'Всего, (ед)',
  'В работе, (ед)',
  'Базис оценки',
  '',
  'Размерность',
  'UnitId',
  'DirectionId',
  'NormType',
];

const rows = [
  ['', '', '', '', '', '', 'Eq Valid', '', '', '', '', '', '', '1', '', '', '', '', '1', '1', 'NT1'],
  ['', '', '', '', '', '', 'Eq Bad Unit', '', '', '', '', '', '', '2', '', '', '', '', '999', '', ''],
  ['', '', '', '', '', '', 'Eq Bad Direction', '', '', '', '', '', '', '3', '', '', '', '', '1', '888', ''],
];

function colName(n) {
  let s = '';
  let x = n;
  while (x > 0) {
    const m = (x - 1) % 26;
    s = String.fromCharCode(65 + m) + s;
    x = Math.floor((x - 1) / 26);
  }
  return s;
}

function cellRef(col, row) {
  return `${colName(col)}${row}`;
}

function escapeXml(value) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function sheetXml() {
  const sheetRows = [headers, ...rows];
  const cells = [];
  sheetRows.forEach((row, rowIndex) => {
    row.forEach((value, colIndex) => {
      if (value === '') return;
      const ref = cellRef(colIndex + 1, rowIndex + 1);
      const isNumber = /^-?\d+(\.\d+)?$/.test(value);
      if (isNumber) {
        cells.push(`<c r="${ref}"><v>${value}</v></c>`);
      } else {
        cells.push(`<c r="${ref}" t="inlineStr"><is><t>${escapeXml(value)}</t></is></c>`);
      }
    });
  });

  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    <row r="1">${cells.filter((c) => c.includes('r="A1"') || c.match(/r="[A-U]1"/)).join('')}</row>
  </sheetData>
</worksheet>`;
}

// Упрощённая генерация: построим sheetData построчно
function buildSheetXml() {
  const sheetRows = [headers, ...rows];
  const rowXml = sheetRows
    .map((row, rowIndex) => {
      const rowNumber = rowIndex + 1;
      const cellXml = row
        .map((value, colIndex) => {
          if (value === '') return '';
          const ref = cellRef(colIndex + 1, rowNumber);
          const isNumber = /^-?\d+(\.\d+)?$/.test(value);
          return isNumber
            ? `<c r="${ref}"><v>${value}</v></c>`
            : `<c r="${ref}" t="inlineStr"><is><t>${escapeXml(value)}</t></is></c>`;
        })
        .filter(Boolean)
        .join('');
      return `<row r="${rowNumber}">${cellXml}</row>`;
    })
    .join('');

  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>${rowXml}</sheetData>
</worksheet>`;
}

function crc32(buf) {
  let crc = -1;
  for (let i = 0; i < buf.length; i++) {
    crc ^= buf[i];
    for (let j = 0; j < 8; j++) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }
  return (crc ^ -1) >>> 0;
}

function zipLocalFile(name, data) {
  const nameBuf = Buffer.from(name, 'utf8');
  const compressed = deflateRawSync(data);
  const header = Buffer.alloc(30 + nameBuf.length);
  header.writeUInt32LE(0x04034b50, 0);
  header.writeUInt16LE(20, 4);
  header.writeUInt16LE(0, 6);
  header.writeUInt16LE(8, 8);
  header.writeUInt16LE(0, 10);
  header.writeUInt16LE(0, 12);
  header.writeUInt32LE(crc32(data), 14);
  header.writeUInt32LE(compressed.length, 18);
  header.writeUInt32LE(data.length, 22);
  header.writeUInt16LE(nameBuf.length, 26);
  header.writeUInt16LE(0, 28);
  nameBuf.copy(header, 30);
  return Buffer.concat([header, compressed]);
}

function zipCentralDir(entries) {
  let offset = 0;
  const localParts = [];
  const centralParts = [];

  for (const entry of entries) {
    const local = zipLocalFile(entry.name, entry.data);
    localParts.push(local);

    const nameBuf = Buffer.from(entry.name, 'utf8');
    const central = Buffer.alloc(46 + nameBuf.length);
    central.writeUInt32LE(0x02014b50, 0);
    central.writeUInt16LE(20, 4);
    central.writeUInt16LE(20, 6);
    central.writeUInt16LE(0, 8);
    central.writeUInt16LE(8, 10);
    central.writeUInt16LE(0, 12);
    central.writeUInt16LE(0, 14);
    central.writeUInt32LE(crc32(entry.data), 16);
    central.writeUInt32LE(deflateRawSync(entry.data).length, 20);
    central.writeUInt32LE(entry.data.length, 24);
    central.writeUInt16LE(nameBuf.length, 28);
    central.writeUInt16LE(0, 30);
    central.writeUInt16LE(0, 32);
    central.writeUInt16LE(0, 34);
    central.writeUInt16LE(0, 36);
    central.writeUInt32LE(0, 38);
    central.writeUInt32LE(offset, 42);
    nameBuf.copy(central, 46);
    centralParts.push(central);
    offset += local.length;
  }

  const centralStart = offset;
  const centralBlob = Buffer.concat(centralParts);
  offset += centralBlob.length;

  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0);
  end.writeUInt16LE(0, 4);
  end.writeUInt16LE(0, 6);
  end.writeUInt16LE(entries.length, 8);
  end.writeUInt16LE(entries.length, 10);
  end.writeUInt32LE(centralBlob.length, 12);
  end.writeUInt32LE(centralStart, 16);
  end.writeUInt16LE(0, 20);

  return Buffer.concat([...localParts, centralBlob, end]);
}

const contentTypes = Buffer.from(`<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>`);

const rels = Buffer.from(`<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>`);

const workbook = Buffer.from(`<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets><sheet name="Equipment" sheetId="1" r:id="rId1"/></sheets>
</workbook>`);

const workbookRels = Buffer.from(`<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>`);

const sheet = Buffer.from(buildSheetXml(), 'utf8');

const zip = zipCentralDir([
  { name: '[Content_Types].xml', data: contentTypes },
  { name: '_rels/.rels', data: rels },
  { name: 'xl/workbook.xml', data: workbook },
  { name: 'xl/_rels/workbook.xml.rels', data: workbookRels },
  { name: 'xl/worksheets/sheet1.xml', data: sheet },
]);

writeFileSync(outPath, zip);
console.log(`Created ${outPath}`);
