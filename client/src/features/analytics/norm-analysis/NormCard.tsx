import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  List,
  ListItem,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  archiveNorm,
  fetchNormRegistryDetails,
  type NormDetails,
  type NormListItem,
} from '../../../api/normRegistryApi';
import { previewNormCurve, type NormPreviewPoint } from '../../../api/normsApi';
import { NormBandChart } from '../../norms/NormBandChart';
import { NormTripleMetrics } from '../../norms/NormTripleMetrics';
import { isKimCurveType, kimDeviation } from '../../norms/kimCurve';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { formatDate } from '../../../utils/formatDateTime';
import type { DeviationFormulaDto, FormulaType, NormFormulaDto } from '../../../types/norms';
import { ConfirmArchiveDialog } from './ConfirmArchiveDialog';

interface Props {
  open: boolean;
  normId: number | null;
  canEdit: boolean;
  onClose: () => void;
  onArchived: () => void;
  variant?: 'dialog' | 'inline';
  onOpenFactNorms?: (groupId: number) => void;
}

const STATUS_RU: Record<string, string> = {
  Active: 'Активна',
  Historical: 'Историческая',
  Future: 'Будущая',
};

const FORMULA_NAME_TO_TYPE: Record<string, FormulaType> = {
  Linear: 1,
  Logarithmic: 2,
  ShiftedLog: 3,
  Wright: 4,
  Power: 5,
  ExponentialDecay: 6,
  PiecewiseLinear: 7,
  Polynomial: 8,
  Constant: 9,
  Hyperbolic: 10,
  Exponential: 11,
  KimCurve: 12,
};

function resolveFormulaType(raw: string): FormulaType {
  if (FORMULA_NAME_TO_TYPE[raw]) return FORMULA_NAME_TO_TYPE[raw];
  const n = Number(raw);
  if (n >= 1 && n <= 12) return n as FormulaType;
  return 1;
}

export function NormCard({
  open,
  normId,
  canEdit,
  onClose,
  onArchived,
  variant = 'dialog',
  onOpenFactNorms,
}: Props) {
  const navigate = useNavigate();
  const [details, setDetails] = useState<NormDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [band, setBand] = useState<NormPreviewPoint[]>([]);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [archiving, setArchiving] = useState(false);

  useEffect(() => {
    if (!open || normId == null) {
      setDetails(null);
      setBand([]);
      return;
    }
    const ac = new AbortController();
    setLoading(true);
    setError(null);
    void fetchNormRegistryDetails(normId, ac.signal)
      .then(async (d) => {
        setDetails(d);
        const formulaType = resolveFormulaType(d.norm.formulaType);
        const a = d.norm.parameters.a ?? d.norm.meanPerformance ?? 1;
        const sigma = d.norm.parameters.sigma ?? d.norm.stdDeviation ?? 0.1;
        const normFormula: NormFormulaDto = {
          type: formulaType,
          coefficients: { a: Number(a), b: 0, ...d.norm.parameters },
          minVolume: 0,
          maxVolume: Math.max(100, Number(a) * 20),
          extrapolation: 2,
        };
        const deviationFormula: DeviationFormulaDto = isKimCurveType(formulaType)
          ? kimDeviation(Number(sigma))
          : {
              modelType: 1,
              coefficients: { sigma: d.norm.stdDeviation ?? 0.1 },
            };
        try {
          const preview = await previewNormCurve(normFormula, deviationFormula, 40);
          setBand(preview.band ?? []);
        } catch {
          setBand([]);
        }
      })
      .catch((err: unknown) => {
        if ((err as { name?: string })?.name === 'CanceledError') return;
        setError(err instanceof Error ? err.message : 'Не удалось загрузить карточку');
      })
      .finally(() => setLoading(false));
    return () => ac.abort();
  }, [open, normId]);

  const norm: NormListItem | null = details?.norm ?? null;
  const canArchive = canEdit && norm?.status !== 'Historical';

  const handleEdit = () => {
    if (!norm) return;
    navigate(`/references/iic-norms?workDirectionId=${norm.groupId}&normId=${norm.id}`);
  };

  const handleArchive = async () => {
    if (!norm) return;
    setArchiving(true);
    try {
      await archiveNorm(norm.id);
      setArchiveOpen(false);
      onArchived();
      onClose();
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? (err instanceof Error ? err.message : 'Ошибка закрытия периода');
      setError(msg);
    } finally {
      setArchiving(false);
    }
  };

  const title = (
    <>
      Норма #{norm?.id ?? '…'}
      {norm ? (
        <Chip size="small" sx={{ ml: 1 }} label={STATUS_RU[norm.status] ?? norm.status} />
      ) : null}
    </>
  );

  const content = (
    <>
      {loading && <Typography color="text.secondary">Загрузка…</Typography>}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      {details && norm ? (
        <Stack spacing={2}>
          <Typography>
            <strong>Группа:</strong>{' '}
            {formatIisNorm({
              code: norm.groupCode,
              name: norm.groupName,
              unitName: details.unitName,
            })}
          </Typography>
          <Typography>
            <strong>Формула:</strong> {norm.formulaType} — {norm.formulaDisplay}
          </Typography>
          <NormTripleMetrics
            performance={norm.meanPerformance}
            mu={norm.estimate ?? (typeof norm.parameters.mu === 'number' ? norm.parameters.mu : null)}
            sigma={norm.stdDeviation}
          />
          <Typography>
            <strong>Период:</strong> {formatDate(norm.effectiveFrom)} —{' '}
            {norm.effectiveTo ? formatDate(norm.effectiveTo) : '∞'}
          </Typography>
          <Typography>
            <strong>Использование:</strong> {details.usage.totalVersions} версий (план{' '}
            {details.usage.planVersions}, факт {details.usage.factVersions}, основных{' '}
            {details.usage.primaryVersions})
          </Typography>

          {band.length > 0 && (
            <Box data-testid="norm-analysis-band-chart">
              <NormBandChart
                band={band}
                deviationRatio={norm.stdDeviation ?? 0.1}
                chartVisible={open}
              />
            </Box>
          )}

          {(details.issues?.length ?? 0) > 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Проблемы health-check
              </Typography>
              <List dense>
                {details.issues.map((i) => (
                  <ListItem key={i.id}>
                    <ListItemText
                      primary={`${i.severity}: ${i.ruleCode}`}
                      secondary={i.message}
                    />
                  </ListItem>
                ))}
              </List>
            </Box>
          )}

          {details.auditHistory.length > 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Аудит
              </Typography>
              <List dense>
                {details.auditHistory.slice(0, 10).map((a) => (
                  <ListItem key={a.id}>
                    <ListItemText
                      primary={`${formatDate(a.timestamp)} — ${a.changedBy}`}
                      secondary={a.newValue ?? a.action}
                    />
                  </ListItem>
                ))}
              </List>
            </Box>
          )}
        </Stack>
      ) : null}
    </>
  );

  const actions = (
    <>
      {variant === 'dialog' ? <Button onClick={onClose}>Закрыть</Button> : null}
      {onOpenFactNorms && norm ? (
        <Button
          onClick={() => onOpenFactNorms(norm.groupId)}
          data-testid="norm-card-open-fact-norms"
        >
          Открыть в анализе факта и норм
        </Button>
      ) : null}
      <Button onClick={handleEdit} disabled={!norm} data-testid="norm-card-edit">
        Открыть в справочнике
      </Button>
      {canArchive && (
        <Button
          color="warning"
          variant="contained"
          onClick={() => setArchiveOpen(true)}
          data-testid="norm-card-archive"
        >
          Закрыть период
        </Button>
      )}
    </>
  );

  return (
    <>
      {variant === 'inline' ? (
        open ? (
          <Box
            component="section"
            data-testid="norm-analysis-card"
            sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2 }}
          >
            <Typography variant="h6" sx={{ mb: 1.5 }}>
              {title}
            </Typography>
            {content}
            <Stack direction="row" spacing={1} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>
              {actions}
            </Stack>
          </Box>
        ) : null
      ) : (
        <Dialog
          open={open}
          onClose={onClose}
          maxWidth="md"
          fullWidth
          data-testid="norm-analysis-card"
        >
          <DialogTitle>{title}</DialogTitle>
          <DialogContent dividers>{content}</DialogContent>
          <DialogActions>{actions}</DialogActions>
        </Dialog>
      )}

      <ConfirmArchiveDialog
        open={archiveOpen}
        norm={norm}
        busy={archiving}
        onClose={() => setArchiveOpen(false)}
        onConfirm={() => void handleArchive()}
      />
    </>
  );
}
