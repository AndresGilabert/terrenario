import { HttpError, type HttpClient } from './http-client';
import type { CreateSeasonPayload, Season } from '../types/season.types';

/**
 * Servicio de temporadas sobre el cliente HTTP común (migrado en MVP-202, P-018). El manejo de
 * 401/403 de ámbito de Workspace vive ahora en el cliente; aquí solo queda la lógica propia del
 * recurso (p. ej. tratar un 404 como "el Workspace aún no tiene temporada").
 */
export function createSeasonService(http: HttpClient) {
  return {
    /** Temporada activa del Workspace en curso. `null` si aún no hay ninguna (RN-021/RN-022). */
    async getActiveSeason(): Promise<Season | null> {
      try {
        return await http.request<Season>('/api/v1/seasons/active');
      } catch (error) {
        if (error instanceof HttpError && error.status === 404) return null;
        throw error;
      }
    },

    /** Crea la (primera) temporada activa del Workspace (MVP-201). */
    async createSeason(payload: CreateSeasonPayload): Promise<Season> {
      return http.request<Season>('/api/v1/seasons', { method: 'POST', body: payload });
    },
  };
}

export type SeasonService = ReturnType<typeof createSeasonService>;
