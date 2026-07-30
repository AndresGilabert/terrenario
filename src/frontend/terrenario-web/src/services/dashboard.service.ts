import type { HttpClient } from './http-client';
import type {
  DashboardFilters,
  DashboardKgByDestination,
  DashboardSummary,
  SeasonProductionResponse,
} from '../types/dashboard.types';

/**
 * Servicio del dashboard (MVP-403) sobre el cliente HTTP común (P-007).
 *
 * Solo lectura: agrega la producción que ya existe. Los filtros son opcionales y el servidor aplica los
 * defectos de RN-008 (temporada activa, todos los terrenos activos), devolviendo el ámbito resuelto en
 * `scope` para que la pantalla sepa de qué son las cifras que enseña.
 */
export function createDashboardService(http: HttpClient) {
  return {
    /** Resumen de temporada: kilos, litros y rendimiento medio ponderado. */
    async getSummary(filters?: DashboardFilters): Promise<DashboardSummary> {
      return http.request<DashboardSummary>('/api/v1/dashboard/summary', {
        query: { season_id: filters?.seasonId, plot_ids: filters?.plotIds },
      });
    },

    /** Kg por destino, con la taxonomía cerrada de RN-012 incluido `desconocido`. */
    async getKgByDestination(filters?: DashboardFilters): Promise<DashboardKgByDestination> {
      return http.request<DashboardKgByDestination>('/api/v1/dashboard/kg-by-destination', {
        query: { season_id: filters?.seasonId, plot_ids: filters?.plotIds },
      });
    },

    /**
     * P-021 — Producción agregada por temporada. Va sin filtros: la tarjeta del maestro habla de la
     * campaña completa, y en una sola petición para no hacer una por temporada.
     */
    async getKgBySeason(): Promise<SeasonProductionResponse> {
      return http.request<SeasonProductionResponse>('/api/v1/dashboard/kg-by-season');
    },
  };
}

export type DashboardService = ReturnType<typeof createDashboardService>;
