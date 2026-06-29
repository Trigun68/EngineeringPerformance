import { ApiContractError } from './apiContract';

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}

/** Извлекает человекочитаемое сообщение из ProblemDetails (RFC 7807) или axios-ошибки. */
export function getApiErrorMessage(error: unknown, fallback = 'Произошла ошибка.'): string {
  if (error instanceof ApiContractError) {
    return `Нарушен контракт API (${error.endpoint}): ${error.issues[0] ?? 'unknown schema error'}`;
  }

  if (typeof error !== 'object' || error === null || !('response' in error)) {
    return fallback;
  }

  const response = (error as { response?: { data?: unknown; status?: number } }).response;
  const data = response?.data;

  if (typeof data === 'object' && data !== null) {
    const problem = data as ApiProblemDetails;
    if (problem.detail && problem.detail.trim().length > 0) {
      return problem.detail;
    }
    if (problem.title && problem.title.trim().length > 0) {
      return problem.title;
    }
    if (problem.errors) {
      const firstEntry = Object.entries(problem.errors)[0];
      if (firstEntry) {
        const [field, messages] = firstEntry;
        const firstMessage = messages[0];
        if (firstMessage) {
          return `${field}: ${firstMessage}`;
        }
      }
    }
  }

  if (typeof data === 'string' && data.trim().length > 0) {
    return data;
  }

  if (response?.status === 401) {
    return 'Требуется авторизация.';
  }
  if (response?.status === 403) {
    return 'Недостаточно прав для выполнения операции.';
  }
  if (response?.status === 404) {
    return 'Ресурс не найден.';
  }
  if (response?.status === 429) {
    return 'Слишком много запросов. Повторите позже.';
  }

  return fallback;
}
