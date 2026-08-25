import type { ButtonProps } from '@mui/material';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import { ACTION_LABELS } from '../../constants/actionLabels';
import { ToolbarActionButton } from './ToolbarActionButton';

export type ExportActionKind =
  | 'excel'
  | 'pdf'
  | 'excelSelected'
  | 'excelSummary'
  | 'csv'
  | 'excelHealth';

const KIND: Record<ExportActionKind, { label: string; Icon: typeof FileDownloadIcon }> = {
  excel: { label: ACTION_LABELS.exportExcel, Icon: FileDownloadIcon },
  pdf: { label: ACTION_LABELS.exportPdf, Icon: PictureAsPdfIcon },
  excelSelected: { label: ACTION_LABELS.exportExcelSelected, Icon: FileDownloadIcon },
  excelSummary: { label: ACTION_LABELS.exportExcelSummary, Icon: FileDownloadIcon },
  csv: { label: ACTION_LABELS.exportCsv, Icon: FileDownloadIcon },
  excelHealth: { label: ACTION_LABELS.exportExcelHealth, Icon: FileDownloadIcon },
};

interface ExportActionButtonProps extends Omit<ButtonProps, 'children' | 'startIcon' | 'variant' | 'size'> {
  kind: ExportActionKind;
  loading?: boolean;
}

export function ExportActionButton({
  kind,
  loading = false,
  disabled,
  ...rest
}: ExportActionButtonProps) {
  const { label, Icon } = KIND[kind];
  return (
    <ToolbarActionButton
      startIcon={<Icon fontSize="small" />}
      loading={loading}
      disabled={disabled}
      aria-label={label}
      {...rest}
    >
      {label}
    </ToolbarActionButton>
  );
}
