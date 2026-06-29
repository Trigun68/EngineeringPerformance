import { describe, expect, it } from 'vitest';
import {
  buildPermissionMatrixRows,
  INDENT_PX_PER_LEVEL,
  isMatrixRowVisible,
  PROJECTS_GROUP_ID,
  type PermissionMatrixRowModel,
} from '../permissionObjectHierarchy';
import type { PermissionMatrix } from '../../api/permissionsMatrixApi';

function makeMatrix(
  entries: Record<string, Record<string, 'None' | 'Read' | 'Edit'>>,
): PermissionMatrix {
  return {
    objects: Object.keys(entries),
    objectHierarchy: [],
    roles: [
      { id: 1, name: 'Administrator' },
      { id: 2, name: 'ProjectManager' },
      { id: 3, name: 'Observer' },
    ],
    matrix: entries,
  };
}

describe('buildPermissionMatrixRows', () => {
  it('groups all project permissions under ProjectsGroup', () => {
    const rows = buildPermissionMatrixRows(
      makeMatrix({
        Projects: { '1': 'Edit', '2': 'Edit', '3': 'Read' },
        Assessment: { '1': 'Edit', '2': 'Read', '3': 'Read' },
        Planning: { '1': 'Edit', '2': 'Edit', '3': 'None' },
        Dashboard: { '1': 'Edit', '2': 'Read', '3': 'None' },
      }),
    );

    const group = rows.find((r) => r.objectName === PROJECTS_GROUP_ID);
    expect(group?.isHeader).toBe(true);
    expect(rows.some((r) => r.objectName === 'Assessment' && r.parentGroup === PROJECTS_GROUP_ID)).toBe(true);
    expect(rows.some((r) => r.objectName === 'Planning' && r.level === 1)).toBe(true);
  });

  it('assigns deeper indent level to children', () => {
    const rows = buildPermissionMatrixRows(
      makeMatrix({
        References: { '1': 'Edit', '2': 'Read', '3': 'Read' },
        PhysicalVolumes: { '1': 'Edit', '2': 'Read', '3': 'None' },
        Equipment: { '1': 'Edit', '2': 'None', '3': 'None' },
      }),
    );

    const section = rows.find((r) => r.objectName === 'References');
    const child = rows.find((r) => r.objectName === 'PhysicalVolumes');
    expect(section?.level).toBe(0);
    expect(child?.level).toBe(1);
    expect(child?.hierarchyPath).toEqual(['References', 'PhysicalVolumes']);
  });

  it('includes Settings group and UserActionLog at top level', () => {
    const rows = buildPermissionMatrixRows(
      makeMatrix({
        SettingsPage: { '1': 'Edit', '2': 'Read', '3': 'None' },
        Users: { '1': 'Edit', '2': 'Edit', '3': 'None' },
        UserActionLog: { '1': 'None', '2': 'None', '3': 'None' },
      }),
    );

    const settingsGroup = rows.find((r) => r.objectName === 'SettingsPage');
    const users = rows.find((r) => r.objectName === 'Users');
    const audit = rows.find((r) => r.objectName === 'UserActionLog');

    expect(settingsGroup?.isHeader).toBe(true);
    expect(users?.parentGroup).toBe('SettingsPage');
    expect(users?.level).toBe(1);
    expect(audit?.parentGroup).toBeNull();
    expect(audit?.level).toBe(0);
  });

  it('shows objects even when all roles have no access', () => {
    const rows = buildPermissionMatrixRows(
      makeMatrix({
        Projects: { '1': 'Edit', '2': 'Read', '3': 'Read' },
        Equipment: { '1': 'None', '2': 'None', '3': 'None' },
        UserActionLog: { '1': 'None', '2': 'None', '3': 'None' },
      }),
    );

    expect(rows.some((r) => r.objectName === 'Equipment')).toBe(true);
    expect(rows.some((r) => r.objectName === 'UserActionLog')).toBe(true);
  });

  it('keeps section header when children have no access', () => {
    const rows = buildPermissionMatrixRows(
      makeMatrix({
        References: { '1': 'Edit', '2': 'Read', '3': 'Read' },
        PhysicalVolumes: { '1': 'None', '2': 'None', '3': 'None' },
        Equipment: { '1': 'None', '2': 'None', '3': 'None' },
      }),
    );

    expect(rows.some((r) => r.objectName === 'References')).toBe(true);
    expect(rows.some((r) => r.objectName === 'PhysicalVolumes')).toBe(true);
  });
});

describe('isMatrixRowVisible', () => {
  it('hides descendants when parent group collapsed', () => {
    const child: PermissionMatrixRowModel = {
      id: 'Assessment',
      objectName: 'Assessment',
      displayName: 'Оценка',
      parentGroup: PROJECTS_GROUP_ID,
      level: 1,
      hierarchyPath: [PROJECTS_GROUP_ID, 'Assessment'],
      isHeader: false,
      levels: { '2': 'Read' },
    };

    expect(isMatrixRowVisible(child, new Set())).toBe(true);
    expect(isMatrixRowVisible(child, new Set([PROJECTS_GROUP_ID]))).toBe(false);
  });
});

describe('INDENT_PX_PER_LEVEL', () => {
  it('uses 20px per hierarchy level', () => {
    expect(INDENT_PX_PER_LEVEL).toBe(20);
  });
});
