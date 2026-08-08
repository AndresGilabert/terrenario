import type { HttpClient } from './http-client';
import type {
  DashboardEconomics,
  DashboardFilters,
  DashboardKgByDestination,
  DashboardKgByPlot,
  DashboardSummary,
  DashboardYieldEvolution,
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

    /** Kg por terreno con el orden fijo de RN-011 (kg descendentes, desempate alfabético). */
    async getKgByPlot(filters?: DashboardFilters): Promise<DashboardKgByPlot> {
      return http.request<DashboardKgByPlot>('/api/v1/dashboard/kg-by-plot', {
        query: { season_id: filters?.seasonId, plot_ids: filters?.plotIds },
      });
    },

    /** Evolución de rendimiento en L/100kg por periodo, con la comparativa histórica básica (RN-015). */
    async getYieldEvolution(
      filters?: DashboardFilters,
      granularity: 'month' | 'week' = 'month'
    ): Promise<DashboardYieldEvolution> {
      return http.request<DashboardYieldEvolution>('/api/v1/dashboard/yield-evolution', {
        query: { season_id: filters?.seasonId, plot_ids: filters?.plotIds, granularity },
      });
    },

    /**
     * MVP-707 — Lectura económica de la campaña sobre el mismo ámbito que el resto de widgets. El
     * gasto se lo pregunta el servidor al diario, así que panel y diario no pueden discrepar (CA-4).
     */
    async getEconomics(filters?: DashboardFilters): Promise<DashboardEconomics> {
      return http.request<DashboardEconomics>('/api/v1/dashboard/economics', {
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
