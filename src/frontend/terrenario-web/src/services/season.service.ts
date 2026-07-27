import { API_BASE, readErrorBody } from './api.config';
import type { CreateSeasonPayload, Season } from '../types/season.types';

export const seasonService = {
  /** Temporada activa del Workspace en curso. `null` si aún no hay ninguna (RN-021/RN-022). */
  async getActiveSeason(accessToken: string): Promise<Season | null> {
    const response = await fetch(`${API_BASE}/api/v1/seasons/active`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (response.status === 404) return null;

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new SeasonServiceError(
        errorBody?.error?.code ?? 'SEASON_FETCH_FAILED',
        errorBody?.error?.message ?? 'No se pudo cargar tu temporada.'
      );
    }

    return response.json();
  },

  /** Crea la (primera) temporada activa del Workspace (MVP-201). */
  async createSeason(payload: CreateSeasonPayload, accessToken: string): Promise<Season> {
    const response = await fetch(`${API_BASE}/api/v1/seasons`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new SeasonServiceError(
        errorBody?.error?.code ?? 'SEASON_CREATE_FAILED',
        errorBody?.error?.message ?? 'No se pudo crear la temporada. Inténtalo de nuevo.'
      );
    }

    return response.json();
  },
};

export class SeasonServiceError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = 'SeasonServiceError';
    this.code = code;
  }
}
