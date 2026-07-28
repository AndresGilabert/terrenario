import type { HttpClient } from './http-client';
import type {
  CreateWorkerPayload,
  UpdateWorkerPayload,
  Worker,
  WorkerListResponse,
} from '../types/worker.types';

/**
 * Servicio del maestro de responsables (MVP-204 · MVP-208) sobre el cliente HTTP común. Como el resto
 * de recursos con ámbito de Workspace, el manejo de 401/403 de scope vive en el cliente (P-007); aquí
 * solo queda la forma del recurso.
 *
 * Desde MVP-208 (CA-2) el listado es la **única** fuente de responsables: incluye a los miembros del
 * Workspace y a la cuadrilla, así que la pantalla ya no combina dos endpoints en cliente.
 */
export function createWorkerService(http: HttpClient) {
  return {
    /** Maestro completo de responsables del Workspace. Filtro opcional por estado de actividad. */
    async listWorkers(params?: { isActive?: boolean }): Promise<Worker[]> {
      const body = await http.request<WorkerListResponse>('/api/v1/workers', {
        query: { is_active: params?.isActive },
      });
      return body.data;
    },

    /** Alta de trabajador de cuadrilla con el dato mínimo obligatorio (nombre). */
    async createWorker(payload: CreateWorkerPayload): Promise<Worker> {
      return http.request<Worker>('/api/v1/workers', { method: 'POST', body: payload });
    },

    /**
     * Edita un trabajador o cambia su estado de actividad (inactivación CA-3). Es un PATCH parcial:
     * en un responsable con cuenta se envía solo `hourly_rate` (MVP-208, CA-4).
     */
    async updateWorker(workerId: string, payload: UpdateWorkerPayload): Promise<Worker> {
      return http.request<Worker>(`/api/v1/workers/${workerId}`, { method: 'PATCH', body: payload });
    },
  };
}

export type WorkerService = ReturnType<typeof createWorkerService>;
