import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import type { ImportDataType, ImportIteration, ImportRowError } from '../../api/importApi';
import {
  fetchImportIterations,
  importProjectData,
  previewLaborImport,
  rollbackImportIteration,
} from '../../api/importApi';
import { usePermission } from '../../hooks/usePermission';
import { useConfirmation } from '../../hooks/useConfirmation';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { FileUploader } from '../FileUploader/FileUploader';

const DATA_TYPES: { value: ImportDataType; label: string }[] = [
  { value: 'physical-volumes', label: 'Физические объёмы' },
  { value: 'labor-intensities', label: 'Трудоёмкости' },
  { value: 'equipment', label: 'Оборудование' },
];

export interface ImportWizardProps {
  projectId: number;
  versionId: number | null;
  onImported?: () => void;
}

function formatRowError(error: ImportRowError): string {
  return `Строка ${error.rowNumber}: ${error.message}`;
}

export function ImportWizard({ projectId, versionId, onImported }: ImportWizardProps) {
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const getDirectionName = useDictionaryStore((s) => s.getDirectionName);
  const dictionariesLoading = useDictionaryStore((s) => s.isLoading);

  const [dataType, setDataType] = useState<ImportDataType>('physical-volumes');
  const [iterations, setIterations] = useState<ImportIteration[]>([]);
  const [preview, setPreview] = useState<string | null>(null);
  const [laborPreview, setLaborPreview] = useState<string[]>([]);
  const [addMissingLabor, setAddMissingLabor] = useState(false);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [result, setResult] = useState<string | null>(null);
  const [rowErrors, setRowErrors] = useState<ImportRowError[]>([]);
  const [error, setError] = useState<string | null>(null);

  const uploadDisabled = !canImport || !versionId || dictionariesLoading;

  const loadIterations = useCallback(async () => {
    if (!versionId) {
      setIterations([]);
      return;
    }
    try {
      setIterations(await fetchImportIterations(projectId, versionId, dataType));
    } catch {
      setIterations([]);
    }
  }, [projectId, versionId, dataType]);

  useEffect(() => {
    void loadIterations();
    setPreview(null);
    setLaborPreview([]);
    setPendingFile(null);
    setResult(null);
    setRowErrors([]);
    setError(null);
  }, [loadIterations, dataType, versionId]);

  const handleFileSelected = async (file: File) => {
    if (!versionId) {
      return;
    }
    setError(null);
    setResult(null);
    setRowErrors([]);
    setPendingFile(file);

    if (dataType === 'labor-intensities') {
      const p = await previewLaborImport(projectId, versionId, file);
      setLaborPreview(p.unmatchedDocumentTypes);
      setPreview(`Строк в файле: ${p.totalDataRows}. Не найдено в справочнике: ${p.unmatchedDocumentTypes.length}`);
    } else {
      setPreview(`Файл: ${file.name}`);
      setLaborPreview([]);
    }
  };

  const handleConfirmImport = async () => {
    if (!pendingFile || !versionId || !canImport) {
      return;
    }
    setError(null);
    setRowErrors([]);
    try {
      const res = await importProjectData(projectId, versionId, dataType, pendingFile, {
        addMissingLaborToCatalog: addMissingLabor,
      });
      setResult(`Импортировано: ${res.importedRows}${res.details ? `\n${res.details}` : ''}`);
      setRowErrors(res.rowErrors ?? []);
      setPendingFile(null);
      await loadIterations();
      onImported?.();
    } catch {
      setError('Ошибка импорта. Проверьте формат файла и права доступа.');
    }
  };

  const handleRollback = async (importedAt: string) => {
    if (!versionId || !canImport) {
      return;
    }
    if (!(await confirm({
      title: 'Откатить итерацию?',
      description: 'Откатить эту итерацию импорта?',
    }))) {
      return;
    }
    const deleted = await rollbackImportIteration(projectId, versionId, dataType, importedAt);
    setResult(`Удалено записей: ${deleted}`);
    await loadIterations();
    onImported?.();
  };

  const rowErrorLines = useMemo(
    () => rowErrors.map(formatRowError),
    [rowErrors],
  );

  const dictionaryHint = useMemo(() => {
    if (dataType !== 'equipment' || !preview) {
      return null;
    }
    return `Справочники: единицы (${getUnitName(1) || '—'}), направления (${getDirectionName(1) || '—'})`;
  }, [dataType, preview, getDirectionName, getUnitName]);

  if (!canImport) {
    return (
      <Alert severity="info">
        Импорт данных недоступен: недостаточно прав (требуется ProjectDataUpload — Edit).
      </Alert>
    );
  }

  if (!versionId) {
    return (
      <Alert severity="info">Выберите версию анализа для импорта данных.</Alert>
    );
  }

  return (
    <Stack spacing={2} maxWidth={640}>
      <Select
        size="small"
        value={dataType}
        onChange={(e) => setDataType(e.target.value as ImportDataType)}
        disabled={dictionariesLoading}
      >
        {DATA_TYPES.map((t) => (
          <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>
        ))}
      </Select>

      <FileUploader
        label="Выбрать Excel"
        onUpload={handleFileSelected}
        disabled={uploadDisabled}
      />

      {dataType === 'labor-intensities' && laborPreview.length > 0 && (
        <Box>
          <FormControlLabel
            control={
              <Checkbox
                checked={addMissingLabor}
                onChange={(e) => setAddMissingLabor(e.target.checked)}
              />
            }
            label="Добавить отсутствующие типы документов в справочник"
          />
          <Typography variant="caption" color="text.secondary" display="block">
            Не найдены: {laborPreview.slice(0, 5).join(', ')}
            {laborPreview.length > 5 ? ` … (+${laborPreview.length - 5})` : ''}
          </Typography>
        </Box>
      )}

      {preview && <Alert severity="info">{preview}</Alert>}
      {dictionaryHint && (
        <Typography variant="caption" color="text.secondary">
          {dictionaryHint}
        </Typography>
      )}
      {pendingFile && (
        <Button
          variant="contained"
          disabled={!canImport || !versionId}
          onClick={() => void handleConfirmImport()}
        >
          Подтвердить импорт
        </Button>
      )}
      {result && <Alert severity="success" sx={{ whiteSpace: 'pre-wrap' }}>{result}</Alert>}
      {rowErrors.length > 0 && (
        <Alert
          severity="warning"
          role="alert"
          aria-label="Ошибки строк импорта"
        >
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            Часть строк пропущена (стратегия Replace):
          </Typography>
          <List dense disablePadding>
            {rowErrorLines.map((line) => (
              <ListItem key={line} disablePadding sx={{ py: 0.25 }}>
                <ListItemText primary={line} primaryTypographyProps={{ variant: 'body2' }} />
              </ListItem>
            ))}
          </List>
        </Alert>
      )}
      {error && <Alert severity="error">{error}</Alert>}

      <Typography variant="subtitle2">История импорта</Typography>
      {iterations.length === 0 ? (
        <Typography variant="body2" color="text.secondary">Нет итераций</Typography>
      ) : (
        <ul style={{ margin: 0, paddingLeft: 20 }}>
          {iterations.map((it) => (
            <li key={it.importedAt}>
              {new Date(it.importedAt).toLocaleString()} — {it.recordCount} записей
              {it.sourceFileName ? ` (${it.sourceFileName})` : ''}
              {' '}
              <Button
                size="small"
                disabled={!canImport}
                onClick={() => void handleRollback(it.importedAt)}
              >
                Откат
              </Button>
            </li>
          ))}
        </ul>
      )}
      {ConfirmDialog}
    </Stack>
  );
}
