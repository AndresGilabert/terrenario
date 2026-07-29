import type { HttpClient } from './http-client';
import type {
  Activity,
  ActivityFilters,
  ActivityListResponse,
  CreateActivityPayload,
  UpdateActivityPayload,
} from '../types/activity.types';

/**
 * Servicio del diario de actividades (MVP-301) sobre el cliente HTTP común (P-007).
 *
 * Es el primer recurso **operativo crítico** del producto: `PATCH` y `DELETE` viajan con `If-Match`
 * y la versión vigente del registro (ADR-0005), de modo que dos personas del mismo Workspace no
 * puedan pisarse una corrección en silencio. Si la versión está desfasada, la API responde
 * `409 CONFLICT_VERSION_MISMATCH` y el `HttpError` llega con ese código para que la vista pueda
 * refrescar en vez de dejar al usuario sin salida.
 */
export function createActivityService(http: HttpClient) {
  return {
    /** Actividades del Workspace por fecha de negocio descendente (RN-033). */
    async listActivities(filters?: ActivityFilters): Promise<Activity[]> {
      const body = await http.request<ActivityListResponse>('/api/v1/activities', {
        query: {
          from: filters?.from,
          to: filters?.to,
          plot_id: filters?.plotId,
          season_id: filters?.seasonId,
          worker_id: filters?.workerId,
        },
      });
      return body.data;
    },

    async createActivity(payload: CreateActivityPayload): Promise<Activity> {
      return http.request<Activity>('/api/v1/activities', { method: 'POST', body: payload });
    },

    /** Corrige una actividad. `version` es la que el cliente cree vigente (ADR-0005). */
    async updateActivity(
      activityId: string,
      version: number,
      payload: UpdateActivityPayload
    ): Promise<Activity> {
      return http.request<Activity>(`/api/v1/activities/${activityId}`, {
        method: 'PATCH',
        body: payload,
        headers: { 'If-Match': String(version) },
      });
    },

    /**
     * MVP-302 — Guarda en el catálogo la tarea libre de una actividad **ya registrada**, sin
     * reescribirla: la API usa el `task_text` que la actividad ya tiene y la deja referenciando la
     * tarea del catálogo. Exige `If-Match` como cualquier otra edición (ADR-0005).
     */
    async saveTaskToCatalog(activityId: string, version: number): Promise<Activity> {
      return http.request<Activity>(`/api/v1/activities/${activityId}`, {
        method: 'PATCH',
        body: { save_task_to_catalog: true },
        headers: { 'If-Match': String(version) },
      });
    },

    /** Eliminación **lógica** (RN-037). La confirmación explícita la pide la UI antes de llamar. */
    async deleteActivity(activityId: string, version: number): Promise<void> {
      await http.request<void>(`/api/v1/activities/${activityId}`, {
        method: 'DELETE',
        headers: { 'If-Match': String(version) },
      });
    },
  };
}

export type ActivityService = ReturnType<typeof createActivityService>;
