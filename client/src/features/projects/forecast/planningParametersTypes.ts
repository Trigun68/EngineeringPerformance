import type { PlanningScenario } from '../../../types/norms';

export interface PlanningParametersDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  planningScenario?: PlanningScenario;
  /** Read-only столбец «Факт» (Z из performance текущей версии) — для вкладки «Оценка факта». */
  showFactColumn?: boolean;
  onClose: () => void;
  onSaved?: () => void;
}

export interface UsePlanningParametersParams {
  open: boolean;
  projectId: number;
  versionId: number | null;
  planningScenario: PlanningScenario;
  showFactColumn: boolean;
  onClose: () => void;
  onSaved?: () => void;
}
