import type { HttpClient } from './http-client';
import type {
  CreateWorkerPayload,
  UpdateWorkerPayload,
  Worker,
  WorkerListResponse,
} from '../types/worker.types';

/**
 * Servicio del maestro de trabajadores (MVP-204) sobre el cliente HTTP común. Como el resto de
 * recursos con ámbito de Workspace, el manejo de 401/403 de scope vive en el cliente (P-007); aquí
 * solo queda la forma del recurso.
 */
export function createWorkerService(http: HttpClient) {
  return {
    /** Lista los trabajadores del Workspace. Filtro opcional por estado de actividad. */
    async listWorkers(params?: { isActive?: boolean }): Promise<Worker[]> {
      const body = await http.request<WorkerListResponse>('/api/v1/workers', {
        query: { is_active: params?.isActive },
      });
      return body.data;
    },

    /** Alta de trabajador con el dato mínimo obligatorio (nombre). */
    async createWorker(payload: CreateWorkerPayload): Promise<Worker> {
      return http.request<Worker>('/api/v1/workers', { method: 'POST', body: payload });
    },

    /** Edita un trabajador o cambia su estado de actividad (inactivación CA-3). */
    async updateWorker(workerId: string, payload: UpdateWorkerPayload): Promise<Worker> {
      return http.request<Worker>(`/api/v1/workers/${workerId}`, { method: 'PATCH', body: payload });
    },
  };
}

export type WorkerService = ReturnType<typeof createWorkerService>;
