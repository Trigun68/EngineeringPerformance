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

const GENERIC_PROBLEM_DETAILS = new Set([
  'One or more validation errors occurred.',
  'Invalid request.',
  'Invalid operation.',
  'Validation failed.',
]);

function formatFieldErrors(errors: Record<string, string[]>): string | null {
  const parts: string[] = [];
  for (const [field, messages] of Object.entries(errors)) {
    const firstMessage = messages[0];
    if (!firstMessage) continue;
    // FluentValidation root RuleFor(x => x) → key "body" / "" — без префикса поля.
    if (!field || field === 'body' || field === '$') {
      parts.push(firstMessage);
    } else {
      parts.push(`${field}: ${firstMessage}`);
    }
  }
  return parts.length > 0 ? parts.join(' ') : null;
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
    // Сначала field errors: detail часто общий английский текст от ApiBehavior.
    if (problem.errors) {
      const fromErrors = formatFieldErrors(problem.errors);
      if (fromErrors) return fromErrors;
    }
    if (
      problem.detail &&
      problem.detail.trim().length > 0 &&
      !GENERIC_PROBLEM_DETAILS.has(problem.detail.trim())
    ) {
      return problem.detail;
    }
    if (
      problem.title &&
      problem.title.trim().length > 0 &&
      !GENERIC_PROBLEM_DETAILS.has(problem.title.trim())
    ) {
      return problem.title;
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
