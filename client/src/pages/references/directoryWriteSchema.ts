import { z } from 'zod';

/** Zod-схема write-path справочников (C-03). */
export const directoryWriteSchema = z.object({
  name: z.string().trim().min(1, 'Наименование обязательно').max(500),
  parentId: z.number().int().positive().nullable().optional(),
  sortOrder: z.number().int().min(0),
  properties: z.record(z.string(), z.unknown()).optional(),
});

export type DirectoryWriteInput = z.infer<typeof directoryWriteSchema>;

export function validateDirectoryWrite(input: unknown): DirectoryWriteInput {
  return directoryWriteSchema.parse(input);
}
