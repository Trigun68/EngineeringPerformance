import { useRef, useState } from 'react';
import { Stack, Typography } from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { ACTION_LABELS } from '../../constants/actionLabels';
import { ToolbarActionButton } from '../export/ToolbarActionButton';

const EXCEL_EXTENSIONS = ['.xlsx', '.xls'];

function isExcelFile(file: File): boolean {
  const lowerName = file.name.toLowerCase();
  return EXCEL_EXTENSIONS.some((ext) => lowerName.endsWith(ext));
}

export interface FileUploaderProps {
  onUpload: (file: File) => Promise<void>;
  disabled?: boolean;
  label?: string;
  accept?: string;
}

export function FileUploader({
  onUpload,
  disabled = false,
  label = ACTION_LABELS.importExcel,
  accept = '.xlsx,.xls,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel',
}: FileUploaderProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    if (!isExcelFile(file)) {
      setError('Выберите файл Excel (.xlsx или .xls).');
      setSelectedFile(null);
      return;
    }

    setError(null);
    setSelectedFile(file);
    setIsUploading(true);
    try {
      await onUpload(file);
      setSelectedFile(null);
    } catch {
      setError('Ошибка при загрузке файла.');
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        hidden
        onChange={handleFileChange}
      />
      <ToolbarActionButton
        startIcon={<UploadFileIcon />}
        loading={isUploading}
        disabled={disabled}
        onClick={() => inputRef.current?.click()}
      >
        {label}
      </ToolbarActionButton>
      {selectedFile && !error && (
        <Typography variant="body2" color="text.secondary">
          {selectedFile.name}
        </Typography>
      )}
      {error && (
        <Typography variant="body2" color="error">
          {error}
        </Typography>
      )}
    </Stack>
  );
}
