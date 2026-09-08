import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Box, Button, Typography } from '@mui/material';

interface ErrorBoundaryProps {
  children: ReactNode;
  /** Смена ключа (например pathname) сбрасывает ошибку — вложенный boundary не залипает после ухода со сломанной панели. */
  resetKey?: string;
}

interface ErrorBoundaryState {
  error: Error | null;
}

/** Ловит необработанные ошибки рендера, чтобы не оставлять белый экран. */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(error, info.componentStack);
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps) {
    if (this.state.error && prevProps.resetKey !== this.props.resetKey) {
      this.setState({ error: null });
    }
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <Box sx={{ p: 4, maxWidth: 560 }}>
        <Typography variant="h6" gutterBottom>
          Не удалось отобразить страницу
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          {this.state.error.message}
        </Typography>
        <Button variant="outlined" onClick={() => this.setState({ error: null })} sx={{ mr: 1 }}>
          Повторить
        </Button>
        <Button variant="outlined" href="/">
          На главную
        </Button>
      </Box>
    );
  }
}
