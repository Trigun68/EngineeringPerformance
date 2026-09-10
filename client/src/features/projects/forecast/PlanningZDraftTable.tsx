import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import type { ProjectVersionPlanningParameter } from '../../../api/planningParametersApi';
import { formatZDisplay, type ZDraftTriple } from './planningZFormat';
import { isDirectionEditable, type ZParameterLevel, type ZTriple } from './planningZLevels';
import type { ZField } from './usePlanningParameters';

const Z_FIELDS: ZField[] = ['zPessimistic', 'zBase', 'zOptimistic'];

export interface PlanningZDraftTableProps {
  zLevel: ZParameterLevel;
  canEdit: boolean;
  projectEditable: boolean;
  zDrafts: Record<number, ZDraftTriple>;
  projectDisplay: ZTriple;
  projectFactZ: number | null;
  visibleRows: Array<ProjectVersionPlanningParameter & { depth: number }>;
  factZByDirection: Map<number, number | null>;
  showFactColumn: boolean;
  onUpdateProjectField: (field: ZField, raw: string) => void;
  onBlurProject: (field: ZField) => void;
  onUpdateDirectionField: (directionId: number, field: ZField, raw: string) => void;
  onBlurDirection: (directionId: number, field: ZField) => void;
}

export function PlanningZDraftTable({
  zLevel,
  canEdit,
  projectEditable,
  zDrafts,
  projectDisplay,
  projectFactZ,
  visibleRows,
  factZByDirection,
  showFactColumn,
  onUpdateProjectField,
  onBlurProject,
  onUpdateDirectionField,
  onBlurDirection,
}: PlanningZDraftTableProps) {
  return (
    <TableContainer component={Paper} variant="outlined">
      <Table size="small" stickyHeader>
        <TableHead>
          <TableRow>
            <TableCell>Группа физ. объёмов IIC</TableCell>
            <TableCell align="right" sx={{ width: 120 }}>Пессимист.</TableCell>
            <TableCell align="right" sx={{ width: 120 }}>Базовый</TableCell>
            <TableCell align="right" sx={{ width: 120 }}>Оптимист.</TableCell>
            {showFactColumn && (
              <TableCell align="right" sx={{ width: 120 }}>Факт</TableCell>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          <TableRow data-testid="z-project-row">
            <TableCell sx={{ fontWeight: 600 }}>По проекту</TableCell>
            {Z_FIELDS.map((field) => (
              <TableCell key={field} align="right">
                <TextField
                  size="small"
                  type="text"
                  inputMode="decimal"
                  value={zDrafts[0]?.[field] ?? formatZDisplay(projectDisplay[field])}
                  disabled={!projectEditable}
                  onChange={(e) => onUpdateProjectField(field, e.target.value)}
                  onBlur={() => onBlurProject(field)}
                  inputProps={{
                    style: { textAlign: 'right' },
                    readOnly: !projectEditable,
                    'aria-label': `Z ${field} по проекту`,
                  }}
                  sx={{ width: 96 }}
                />
              </TableCell>
            ))}
            {showFactColumn && (
              <TableCell align="right" data-testid="z-project-fact">
                <TextField
                  size="small"
                  value={projectFactZ != null ? formatZDisplay(projectFactZ) : '—'}
                  disabled
                  inputProps={{
                    style: { textAlign: 'right' },
                    readOnly: true,
                    'aria-label': 'Факт Z по проекту',
                  }}
                  sx={{ width: 96 }}
                />
              </TableCell>
            )}
          </TableRow>
          {visibleRows.map((row) => {
            const editable = canEdit && isDirectionEditable(zLevel, row.depth);
            const factZ = factZByDirection.get(row.iicWorkDirectionId);
            const factDisplay =
              factZ != null && Number.isFinite(factZ)
                ? formatZDisplay(factZ)
                : row.zActual != null
                  ? formatZDisplay(row.zActual)
                  : '—';
            return (
              <TableRow key={row.id}>
                <TableCell sx={{ pl: 2 + row.depth * 2.5 }}>
                  {formatIisNorm({ code: row.code, name: row.name, unitName: row.unitName })}
                </TableCell>
                {Z_FIELDS.map((field) => (
                  <TableCell key={field} align="right">
                    <TextField
                      size="small"
                      type="text"
                      inputMode="decimal"
                      value={zDrafts[row.iicWorkDirectionId]?.[field] ?? formatZDisplay(row[field])}
                      disabled={!editable}
                      onChange={(e) => onUpdateDirectionField(row.iicWorkDirectionId, field, e.target.value)}
                      onBlur={() => onBlurDirection(row.iicWorkDirectionId, field)}
                      inputProps={{
                        style: { textAlign: 'right' },
                        'aria-label': `Z ${field} ${row.code}`,
                      }}
                      sx={{ width: 96 }}
                    />
                  </TableCell>
                ))}
                {showFactColumn && (
                  <TableCell align="right">
                    <TextField
                      size="small"
                      value={factDisplay}
                      disabled
                      inputProps={{
                        style: { textAlign: 'right' },
                        readOnly: true,
                        'aria-label': `Факт Z ${row.name}`,
                      }}
                      sx={{ width: 96 }}
                    />
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
