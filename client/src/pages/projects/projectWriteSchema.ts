import { z } from 'zod';

/** Zod write-path для проекта (IMP-07). */
export const createProjectSchema = z.object({
  code: z.string().trim().min(1, 'Код обязателен').max(100),
  name: z.string().trim().min(1, 'Наименование обязательно').max(500),
  type: z.string().trim().max(200).optional().nullable(),
  customer: z.string().trim().max(500).optional().nullable(),
  contractor: z.string().trim().max(500).optional().nullable(),
  program: z.string().trim().max(500).optional().nullable(),
  gip: z.string().trim().max(200).optional().nullable(),
  workStatus: z.string().trim().max(200).optional().nullable(),
});

export const updateProjectSchema = createProjectSchema.partial().extend({
  code: z.string().trim().min(1).max(100).optional(),
  name: z.string().trim().min(1).max(500).optional(),
});

export const createVersionSchema = z.object({
  versionName: z.string().trim().min(1, 'Имя версии обязательно').max(200),
  name: z.string().trim().max(500).optional(),
  description: z.string().trim().max(2000).optional().nullable(),
  direction: z.number().int().min(0).max(2).optional(),
});

export type CreateProjectInput = z.infer<typeof createProjectSchema>;
export type CreateVersionInput = z.infer<typeof createVersionSchema>;
