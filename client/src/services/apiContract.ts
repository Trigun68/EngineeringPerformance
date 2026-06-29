import { z } from 'zod';

export class ApiContractError extends Error {
  public readonly endpoint: string;
  public readonly issues: string[];

  constructor(endpoint: string, issues: string[]) {
    super(`API contract violation at ${endpoint}`);
    this.name = 'ApiContractError';
    this.endpoint = endpoint;
    this.issues = issues;
  }
}

export function parseApiContract<T>(
  endpoint: string,
  schema: z.ZodType<T>,
  payload: unknown,
): T {
  const result = schema.safeParse(payload);
  if (result.success) {
    return result.data;
  }

  const issues = result.error.issues.map((issue) => {
    const path = issue.path.length > 0 ? issue.path.join('.') : 'body';
    return `${path}: ${issue.message}`;
  });

  throw new ApiContractError(endpoint, issues);
}
