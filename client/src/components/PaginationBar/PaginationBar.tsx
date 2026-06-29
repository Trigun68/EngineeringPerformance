import { Box, MenuItem, Stack, TextField, Typography } from '@mui/material';

export interface PaginationBarProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export function PaginationBar({
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
}: PaginationBarProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 1 }}>
      <Typography variant="body2" color="text.secondary">
        Всего: {totalCount}
      </Typography>
      <TextField
        select
        size="small"
        label="Стр."
        value={page}
        onChange={(e) => onPageChange(Number(e.target.value))}
        sx={{ width: 90 }}
      >
        {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
          <MenuItem key={p} value={p}>{p}</MenuItem>
        ))}
      </TextField>
      <TextField
        select
        size="small"
        label="На стр."
        value={pageSize}
        onChange={(e) => {
          onPageSizeChange(Number(e.target.value));
          onPageChange(1);
        }}
        sx={{ width: 110 }}
      >
        {[50, 100, 200, 500].map((s) => (
          <MenuItem key={s} value={s}>{s}</MenuItem>
        ))}
      </TextField>
      <Box flex={1} />
      <Typography variant="body2" color="text.secondary">
        {totalPages} стр.
      </Typography>
    </Stack>
  );
}
