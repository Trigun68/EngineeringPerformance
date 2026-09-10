import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { ZParametersCaption } from './ZParametersCaption';
import { PlanningParametersForm } from './PlanningParametersForm';
import { PlanningZDraftTable } from './PlanningZDraftTable';
import { usePlanningParameters } from './usePlanningParameters';
import { formatZDisplay, parseZInput } from './planningZFormat';
import type { PlanningParametersDialogProps } from './planningParametersTypes';

export type { PlanningParametersDialogProps };
export { formatZDisplay, parseZInput };

export function PlanningParametersDialog({
  open,
  projectId,
  versionId,
  planningScenario = 1,
  showFactColumn = false,
  onClose,
  onSaved,
}: PlanningParametersDialogProps) {
  const p = usePlanningParameters({
    open,
    projectId,
    versionId,
    planningScenario,
    showFactColumn,
    onClose,
    onSaved,
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth data-testid="planning-parameters-dialog">
      <WorkingOverlay open={p.saving || p.loading} delayMs={p.saving ? 0 : 1000} />
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Параметры планирования
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <ZParametersCaption
          variant={showFactColumn ? 'fact' : 'plan'}
          planVersionSelected={Boolean(versionId)}
        />
        <PlanningParametersForm
          scenario={p.scenario}
          onScenarioChange={p.setScenario}
          zLevel={p.zLevel}
          onZLevelChange={p.handleZLevelChange}
          canEdit={p.canEdit}
          saving={p.saving}
        />
        {p.error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {p.error}
          </Alert>
        )}
        {p.loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={28} />
          </Box>
        ) : (
          <PlanningZDraftTable
            zLevel={p.zLevel}
            canEdit={p.canEdit}
            projectEditable={p.projectEditable}
            zDrafts={p.zDrafts}
            projectDisplay={p.projectDisplay}
            projectFactZ={p.projectFactZ}
            visibleRows={p.visibleRows}
            factZByDirection={p.factZByDirection}
            showFactColumn={showFactColumn}
            onUpdateProjectField={p.updateProjectField}
            onBlurProject={p.blurProject}
            onUpdateDirectionField={p.updateDirectionField}
            onBlurDirection={p.blurDirection}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        {p.canEdit && (
          <Button
            variant="contained"
            onClick={() => void p.handleSave()}
            disabled={p.saving || p.loading || !versionId}
          >
            {p.saving ? 'Сохранение…' : 'Сохранить'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
