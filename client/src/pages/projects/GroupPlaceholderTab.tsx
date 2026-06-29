import { useEffect, useState } from 'react';
import { Alert, Typography } from '@mui/material';

interface GroupPlaceholderTabProps {
  projectId: number;
  title: string;
  fetcher: (projectId: number) => Promise<{ message: string }>;
}

export function GroupPlaceholderTab({ projectId, title, fetcher }: GroupPlaceholderTabProps) {
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void fetcher(projectId)
      .then((r) => setMessage(r.message))
      .catch(() => setError('Не удалось загрузить данные.'));
  }, [projectId, fetcher]);

  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Typography variant="body1" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
      {title}: {message ?? 'Загрузка...'}
    </Typography>
  );
}
