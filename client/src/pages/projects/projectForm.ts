import type { Project } from '../../types/project';
import type { ProjectFormData } from '../../types/project';

export const EMPTY_PROJECT_FORM: ProjectFormData = {
  code: '',
  name: '',
  workNumber: '',
  contractNumber: '',
  calendarPlanNumber: '',
  type: '',
  customer: '',
  contractor: '',
  gip: '',
  workStatus: '',
  workStart: '',
  program: '',
  description: '',
};

export function toProjectForm(project: Project): ProjectFormData {
  return {
    code: project.code,
    name: project.name,
    workNumber: project.workNumber ?? '',
    contractNumber: project.contractNumber ?? '',
    calendarPlanNumber: project.calendarPlanNumber ?? '',
    type: project.type ?? '',
    customer: project.customer ?? '',
    contractor: project.contractor ?? '',
    gip: project.gip ?? '',
    workStatus: project.workStatus ?? '',
    workStart: project.workStart ?? '',
    program: project.program ?? '',
    description: project.description ?? '',
  };
}
