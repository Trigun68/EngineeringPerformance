import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import CompareArrowsOutlinedIcon from '@mui/icons-material/CompareArrowsOutlined';
import { useNavigate } from 'react-router-dom';
import {
  archiveNorm,
  fetchNormRegistryDetails,
  type NormDetails,
  type NormHealthIssue,
  type NormListItem,
} from '../../../api/normRegistryApi';
import { previewNormCurve, type NormPreviewPoint } from '../../../api/normsApi';
import { NormBandChart } from '../../norms/NormBandChart';
import { NormTripleMetrics } from '../../norms/NormTripleMetrics';
import { isKimCurveType, kimDeviation, muFromProductivity } from '../../norms/kimCurve';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { formatDate } from '../../../utils/formatDateTime';
import type { DeviationFormulaDto, FormulaType, NormFormulaDto } from '../../../types/norms';
import { ConfirmArchiveDialog } from './ConfirmArchiveDialog';
import { NormWhatIfSimulator } from './NormWhatIfSimulator';
import {
  CONFIDENCE_COLOR,
  CONFIDENCE_LABEL,
  SEVERITY_BG,
  SEVERITY_COLOR,
  SEVERITY_LABEL,
  STATUS_LABEL,
  confidenceOf,
  issuesForNorm,
  previousNormInGroup,
  severityForNorm,
} from './normAnalysisMetrics';

interface Props {
  row: NormListItem;
  allRows: NormListItem[];
  issues: NormHealthIssue[];
  canEdit: boolean;
  factYs: number[] | null;
  meanOffsetPct: number;
  sigmaMultiplier: number;
  onMeanOffset: (value: number) => void;
  onSigmaMultiplier: (value: number) => void;
  onResetSim: () => void;
  onArchived: () => void;
  onOpenFactNorms?: (groupId: number) => void;
}

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

function scaleKimBand(
  band: NormPreviewPoint[],
  nextMean: number,
  nextSigma: number,
): NormPreviewPoint[] {
  if (band.length === 0 || nextMean <= 0) return band;
  const mu = Math.log(nextMean);
  const center = nextMean;
  const upper = Math.exp(mu + nextSigma);
  const lower = Math.exp(mu - nextSigma);
  return band.map((p) => ({ ...p, center, upper, lower }));
}

function scaleBand(
  band: NormPreviewPoint[],
  mean: number,
  nextMean: number,
  nextRatio: number,
): NormPreviewPoint[] {
  if (band.length === 0 || mean === 0) return band;
  const scale = nextMean / mean;
  return band.map((p) => {
    const center = p.center * scale;
    return {
      ...p,
      center,
      lower: center * (1 - nextRatio),
      upper: center * (1 + nextRatio),
    };
  });
}

/** Inline-панель выбранной нормы — композиция NormAnalysisMockup. */
export function NormInlineDetail({
  row,
  allRows,
  issues,
  canEdit,
  factYs,
  meanOffsetPct,
  sigmaMultiplier,
  onMeanOffset,
  onSigmaMultiplier,
  onResetSim,
  onArchived,
  onOpenFactNorms,
}: Props) {
  const navigate = useNavigate();
  const [details, setDetails] = useState<NormDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [band, setBand] = useState<NormPreviewPoint[]>([]);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [archiving, setArchiving] = useState(false);

  useEffect(() => {
    const ac = new AbortController();
    setError(null);
    void fetchNormRegistryDetails(row.id, ac.signal)
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
      });
    return () => ac.abort();
  }, [row.id]);

  const groupLabel = formatIisNorm({
    code: row.groupCode,
    name: row.groupName,
    unitName: details?.unitName,
  });
  const sevKey = severityForNorm(issues, row);
  const confidence = confidenceOf(row);
  const relatedIssues = issuesForNorm(details?.issues?.length ? details.issues : issues, row);
  const previous = previousNormInGroup(allRows, row);
  const mean = row.meanPerformance;
  const sigma = row.stdDeviation;
  const mu = row.estimate
    ?? (typeof row.parameters.mu === 'number' ? row.parameters.mu : null)
    ?? (mean != null && mean > 0 ? muFromProductivity(mean) : null);
  const kim = isKimCurveType(resolveFormulaType(row.formulaType));
  const nextMean = mean == null ? null : mean * (1 + meanOffsetPct / 100);
  const nextSigma = sigma == null ? null : sigma * sigmaMultiplier;
  const nextRatio =
    nextMean != null && nextSigma != null && nextMean !== 0 ? nextSigma / nextMean : 0;
  const proposedBand = useMemo(() => {
    if (mean == null || nextMean == null) return band;
    if (kim && nextSigma != null) return scaleKimBand(band, nextMean, nextSigma);
    return scaleBand(band, mean, nextMean, nextRatio);
  }, [band, kim, mean, nextMean, nextRatio, nextSigma]);

  const canArchive = canEdit && row.status !== 'Historical';
  const primaryIssue = relatedIssues[0];

  const handleArchive = async () => {
    setArchiving(true);
    try {
      await archiveNorm(row.id);
      setArchiveOpen(false);
      onArchived();
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? (err instanceof Error ? err.message : 'Ошибка закрытия периода');
      setError(msg);
    } finally {
      setArchiving(false);
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }} data-testid="norm-analysis-card">
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="flex-start"
        flexWrap="wrap"
        useFlexGap
        spacing={1}
      >
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            {groupLabel}
          </Typography>
          <Stack direction="row" spacing={1} sx={{ mt: 0.75 }} flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              label={SEVERITY_LABEL[sevKey]}
              sx={{
                bgcolor: SEVERITY_BG[sevKey],
                color: SEVERITY_COLOR[sevKey],
                fontWeight: 600,
              }}
            />
            <Chip size="small" variant="outlined" label={STATUS_LABEL[row.status] ?? row.status} />
            <Chip size="small" variant="outlined" label={`Формула: ${row.formulaType}`} />
            <Tooltip title={`N = ${row.totalVersions} версий`}>
              <Chip
                size="small"
                variant="outlined"
                label={`Достоверность: ${CONFIDENCE_LABEL[confidence]}`}
                sx={{ borderColor: CONFIDENCE_COLOR[confidence], color: CONFIDENCE_COLOR[confidence] }}
              />
            </Tooltip>
          </Stack>
        </Box>
        {onOpenFactNorms ? (
          <Button
            size="small"
            variant="outlined"
            startIcon={<CompareArrowsOutlinedIcon fontSize="small" />}
            onClick={() => onOpenFactNorms(row.groupId)}
            data-testid="norm-card-open-fact-norms"
          >
            Показать в сравнении факта и норм
          </Button>
        ) : null}
      </Stack>

      <Box sx={{ mt: 2 }}>
        <NormTripleMetrics performance={mean} mu={mu} sigma={sigma} />
      </Box>

      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {primaryIssue && (
        <Alert
          severity={sevKey === 'Error' ? 'error' : sevKey === 'Warning' ? 'warning' : 'info'}
          sx={{ mt: 2 }}
        >
          <Typography variant="body2">{primaryIssue.message}</Typography>
          {primaryIssue.recommendation ? (
            <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 600 }}>
              Рекомендация: {primaryIssue.recommendation}
            </Typography>
          ) : null}
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mt: 2 }}>
        {previous && previous.meanPerformance != null && mean != null ? (
          <Box sx={{ minWidth: 260, flex: '1 1 260px' }}>
            <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
              Сравнение с предыдущей версией
            </Typography>
            <Stack spacing={0.75}>
              <Stack direction="row" justifyContent="space-between">
                <Typography variant="caption" color="text.secondary">
                  Производительность: {previous.meanPerformance.toFixed(3)} → {mean.toFixed(3)}
                </Typography>
                {previous.meanPerformance !== 0 ? (
                  <Typography variant="caption" sx={{ fontWeight: 600 }}>
                    {mean >= previous.meanPerformance ? '+' : ''}
                    {(((mean - previous.meanPerformance) / previous.meanPerformance) * 100).toFixed(1)}%
                  </Typography>
                ) : null}
              </Stack>
              {previous.stdDeviation != null && sigma != null ? (
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="caption" color="text.secondary">
                    σ: {previous.stdDeviation.toFixed(3)} → {sigma.toFixed(3)}
                  </Typography>
                  {previous.stdDeviation !== 0 ? (
                    <Typography
                      variant="caption"
                      sx={{ fontWeight: 600 }}
                      color={sigma > previous.stdDeviation * 1.2 ? 'error.main' : 'text.secondary'}
                    >
                      {sigma >= previous.stdDeviation ? '+' : ''}
                      {(((sigma - previous.stdDeviation) / previous.stdDeviation) * 100).toFixed(1)}%
                    </Typography>
                  ) : null}
                </Stack>
              ) : null}
              <Typography variant="caption" color="text.secondary">
                Действует с {formatDate(row.effectiveFrom)}
                {row.effectiveTo ? ` по ${formatDate(row.effectiveTo)}` : ' (текущая)'}
              </Typography>
            </Stack>
          </Box>
        ) : (
          <Box sx={{ minWidth: 260, flex: '1 1 260px' }}>
            <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
              Период действия
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {formatDate(row.effectiveFrom)}
              {row.effectiveTo ? ` — ${formatDate(row.effectiveTo)}` : ' — текущая'}
            </Typography>
          </Box>
        )}

        <Box sx={{ minWidth: 320, flex: '2 1 420px' }} data-testid="norm-analysis-band-chart">
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
            Коридор допуска (с учётом симулятора)
          </Typography>
          <NormBandChart band={proposedBand} deviationRatio={nextRatio} chartVisible />
        </Box>
      </Box>

      <Box sx={{ mt: 2 }}>
        <NormWhatIfSimulator
          mean={mean}
          sigma={sigma}
          factYs={factYs}
          meanOffsetPct={meanOffsetPct}
          sigmaMultiplier={sigmaMultiplier}
          onMeanOffset={onMeanOffset}
          onSigmaMultiplier={onSigmaMultiplier}
          onReset={onResetSim}
        />
      </Box>

      <Stack direction="row" spacing={1} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>
        <Button
          size="small"
          onClick={() => navigate(`/references/iic-norms?workDirectionId=${row.groupId}&normId=${row.id}`)}
          data-testid="norm-card-edit"
        >
          Открыть в справочнике
        </Button>
        {canArchive && (
          <Button
            size="small"
            color="warning"
            onClick={() => setArchiveOpen(true)}
            data-testid="norm-card-archive"
          >
            Закрыть период
          </Button>
        )}
      </Stack>

      <ConfirmArchiveDialog
        open={archiveOpen}
        norm={row}
        busy={archiving}
        onClose={() => setArchiveOpen(false)}
        onConfirm={() => void handleArchive()}
      />
    </Paper>
  );
}
