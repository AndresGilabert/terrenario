import { HttpError, type HttpClient } from './http-client';
import type {
  CreateSeasonPayload,
  Season,
  SeasonListResponse,
  UpdateSeasonPayload,
} from '../types/season.types';

/**
 * Servicio de temporadas sobre el cliente HTTP común. Cubre el maestro (MVP-203): listar, crear,
 * editar/cerrar y cambiar la temporada activa, además de la consulta de la activa (MVP-201). El
 * manejo de 401/403 de ámbito de Workspace vive en el cliente; aquí solo queda la lógica propia del
 * recurso (p. ej. tratar un 404 de la activa como "el Workspace aún no tiene temporada").
 */
export function createSeasonService(http: HttpClient) {
  return {
    /** Temporadas del Workspace (activa primero, luego histórico por fecha). */
    async listSeasons(): Promise<Season[]> {
      const body = await http.request<SeasonListResponse>('/api/v1/seasons');
      return body.data;
    },

    /** Temporada activa del Workspace en curso. `null` si aún no hay ninguna (RN-021/RN-022). */
    async getActiveSeason(): Promise<Season | null> {
      try {
        return await http.request<Season>('/api/v1/seasons/active');
      } catch (error) {
        if (error instanceof HttpError && error.status === 404) return null;
        throw error;
      }
    },

    /** Crea una temporada. La nueva pasa a ser la activa del Workspace (MVP-203). */
    async createSeason(payload: CreateSeasonPayload): Promise<Season> {
      return http.request<Season>('/api/v1/seasons', { method: 'POST', body: payload });
    },

    /** Edita nombre/fechas o cierra/reabre una temporada (`is_closed`). Campos parciales. */
    async updateSeason(seasonId: string, payload: UpdateSeasonPayload): Promise<Season> {
      return http.request<Season>(`/api/v1/seasons/${seasonId}`, { method: 'PATCH', body: payload });
    },

    /** Cambia la temporada activa del Workspace: activa la indicada y desbanca a la anterior (RN-022). */
    async activateSeason(seasonId: string): Promise<Season> {
      return http.request<Season>(`/api/v1/seasons/${seasonId}/activate`, { method: 'POST' });
    },
  };
}

export type SeasonService = ReturnType<typeof createSeasonService>;
