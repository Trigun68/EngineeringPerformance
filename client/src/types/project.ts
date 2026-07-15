export interface Project {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  workNumber?: string | null;
  contractNumber?: string | null;
  calendarPlanNumber?: string | null;
  type?: string | null;
  customer?: string | null;
  contractor?: string | null;
  gip?: string | null;
  workStatus?: string | null;
  workStart?: string | null;
  program?: string | null;
  versionCount: number;
  isArchived?: boolean;
}

export type ProjectFormData = Omit<Project, 'id' | 'versionCount'> & {
  id?: number;
};
