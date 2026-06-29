import apiClient from '../services/apiClient';
import type { IicNorm } from './registersApi';

export async function fetchIicNormsByKfo(kfoId: number): Promise<IicNorm[]> {
  const { data } = await apiClient.get<IicNorm[]>('/iic-norms', {
    params: { kfoId },
  });
  return data;
}
