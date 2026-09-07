import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Checkbox,
  FormControlLabel,
  List,
  ListItem,
  ListItemText,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import {
  importProjectData,
  previewLaborImport,
  type ImportDataType,
  type ImportRowError,
} from '../../../api/importApi';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { getApiErrorMessage } from '../../../services/apiError';

export interface DataImportDialogProps {
  open: boolean;
  dataType: ImportDataType;
  projectId: number;
  versionId: number;
  onClose: () => void;
  onSuccess?: () => void;
}

const DATA_TYPE_LABELS: Record<ImportDataType, string> = {
  'physical-volumes': 'физические объёмы',
  equipment: 'оборудование',
  'labor-intensities': 'трудоёмкость',
};

function formatRowError(error: ImportRowError): string {
  return `Строка ${error.rowNumber}: ${error.message}`;
}

export function DataImportDialog({
  open,
  dataType,
  projectId,
  versionId,
  onClose,
  onSuccess,
}: DataImportDialogProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [importing, setImporting] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [rowErrors, setRowErrors] = useState<ImportRowError[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [laborPreview, setLaborPreview] = useState<string[]>([]);
  const [laborPreviewInfo, setLaborPreviewInfo] = useState<string | null>(null);
  const [addMissingLabor, setAddMissingLabor] = useState(true);
  const [previewing, setPreviewing] = useState(false);

  const title = useMemo(
    () => `Импорт: ${DATA_TYPE_LABELS[dataType]}`,
    [dataType],
  );

  const resetState = useCallback(() => {
    setPendingFile(null);
    setImporting(false);
    setResult(null);
    setRowErrors([]);
    setError(null);
    setLaborPreview([]);
    setLaborPreviewInfo(null);
    setAddMissingLabor(true);
    setPreviewing(false);
  }, []);

  useEffect(() => {
    if (open) {
      resetState();
    }
  }, [open, dataType, resetState]);

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    const lowerName = file.name.toLowerCase();
    if (!lowerName.endsWith('.xlsx') && !lowerName.endsWith('.xls')) {
      setError('Выберите файл Excel (.xlsx или .xls).');
      setPendingFile(null);
      return;
    }

    setError(null);
    setResult(null);
    setRowErrors([]);
    setLaborPreview([]);
    setLaborPreviewInfo(null);
    setPendingFile(file);

    if (dataType === 'labor-intensities') {
      setPreviewing(true);
      try {
        const preview = await previewLaborImport(projectId, versionId, file);
        setLaborPreview(preview.unmatchedDocumentTypes);
        setLaborPreviewInfo(
          `Строк в файле: ${preview.totalDataRows}. Не найдено в справочнике: ${preview.unmatchedDocumentTypes.length}`,
        );
      } catch (err) {
        setError(getApiErrorMessage(err, 'Не удалось выполнить предпросмотр файла.'));
        setPendingFile(null);
      } finally {
        setPreviewing(false);
      }
    }
  };

  const handleImport = async () => {
    if (!pendingFile) return;
    setImporting(true);
    setError(null);
    setRowErrors([]);
    try {
      const res = await importProjectData(projectId, versionId, dataType, pendingFile, {
        addMissingLaborToCatalog: dataType === 'labor-intensities' ? addMissingLabor : undefined,
      });
      setResult(`Импортировано записей: ${res.importedRows}${res.details ? `. ${res.details}` : ''}`);
      setRowErrors(res.rowErrors ?? []);
      setPendingFile(null);
      onSuccess?.();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить импорт.'));
    } finally {
      setImporting(false);
    }
  };

  const rowErrorLines = useMemo(
    () => rowErrors.map(formatRowError),
    [rowErrors],
  );

  return (
    <>
      <WorkingOverlay open={importing || previewing} />
      <Dialog
        open={open}
        onClose={() => {
          if (!importing && !previewing) onClose();
        }}
        maxWidth="sm"
        fullWidth
        data-testid="data-import-dialog"
      >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        {title}
        <IconButton aria-label="Закрыть" onClick={onClose} size="small" disabled={importing || previewing}>
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Загрузите Excel-файл для импорта {DATA_TYPE_LABELS[dataType]} в выбранную версию.
        </Typography>

        <input
          ref={inputRef}
          type="file"
          accept=".xlsx,.xls"
          hidden
          onChange={handleFileChange}
        />
        <ToolbarActionButton
          startIcon={<UploadFileIcon />}
          loading={previewing}
          disabled={importing}
          onClick={() => inputRef.current?.click()}
        >
          {ACTION_LABELS.selectFile}
        </ToolbarActionButton>
        {pendingFile && (
          <Typography variant="body2" sx={{ mt: 1 }}>
            Файл: <strong>{pendingFile.name}</strong>
          </Typography>
        )}
        {laborPreviewInfo && (
          <Alert severity="info" sx={{ mt: 2 }}>{laborPreviewInfo}</Alert>
        )}
        {laborPreview.length > 0 && (
          <Alert severity="warning" sx={{ mt: 1 }}>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              Типы не найдены в справочнике:
            </Typography>
            <List dense disablePadding>
              {laborPreview.slice(0, 12).map((name) => (
                <ListItem key={name} disablePadding sx={{ py: 0.25 }}>
                  <ListItemText primary={name} primaryTypographyProps={{ variant: 'body2' }} />
                </ListItem>
              ))}
            </List>
            {laborPreview.length > 12 ? (
              <Typography variant="caption">… и ещё {laborPreview.length - 12}</Typography>
            ) : null}
          </Alert>
        )}
        {dataType === 'labor-intensities' && pendingFile && (
          <FormControlLabel
            sx={{ mt: 1, display: 'block' }}
            control={
              <Checkbox
                checked={addMissingLabor}
                onChange={(e) => setAddMissingLabor(e.target.checked)}
              />
            }
            label="Добавить отсутствующие типы документов в справочник"
          />
        )}

        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        {result && <Alert severity="success" sx={{ mt: 2, whiteSpace: 'pre-wrap' }}>{result}</Alert>}
        {rowErrors.length > 0 && (
          <Alert severity="warning" sx={{ mt: 2 }} role="alert" aria-label="Ошибки строк импорта">
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Часть строк пропущена:
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
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={importing || previewing}>Закрыть</Button>
        <Button
          variant="contained"
          disabled={!pendingFile || importing || previewing}
          onClick={() => void handleImport()}
        >
          {importing ? 'Работаю…' : 'Импортировать'}
        </Button>
      </DialogActions>
    </Dialog>
    </>
  );
}
