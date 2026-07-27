import type { HttpClient } from './http-client';
import type {
  CreatePlotPayload,
  Plot,
  PlotListResponse,
  UpdatePlotPayload,
} from '../types/plot.types';

/**
 * Servicio del maestro de terrenos (MVP-202) sobre el cliente HTTP común. `plots` es el primer
 * recurso con ámbito de Workspace consumido con CRUD por la UI; el manejo de 401/403 de scope vive
 * en el cliente (P-007), así que aquí solo queda la forma del recurso.
 */
export function createPlotService(http: HttpClient) {
  return {
    /** Lista los terrenos del Workspace. Filtros opcionales: búsqueda y estado de actividad. */
    async listPlots(params?: { search?: string; isActive?: boolean }): Promise<Plot[]> {
      const body = await http.request<PlotListResponse>('/api/v1/plots', {
        query: { search: params?.search, is_active: params?.isActive },
      });
      return body.data;
    },

    /** Alta de terreno con los datos mínimos obligatorios (RN-028). */
    async createPlot(payload: CreatePlotPayload): Promise<Plot> {
      return http.request<Plot>('/api/v1/plots', { method: 'POST', body: payload });
    },

    /** Edita un terreno o cambia su estado de actividad (inactivación CA-3). */
    async updatePlot(plotId: string, payload: UpdatePlotPayload): Promise<Plot> {
      return http.request<Plot>(`/api/v1/plots/${plotId}`, { method: 'PATCH', body: payload });
    },
  };
}

export type PlotService = ReturnType<typeof createPlotService>;
