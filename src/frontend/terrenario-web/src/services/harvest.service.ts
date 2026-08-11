import type { HttpClient } from './http-client';
import type {
  CreateHarvestPayload,
  Harvest,
  HarvestFilters,
  HarvestDuplicateListResponse,
  HarvestDuplicateQuery,
  HarvestListResponse,
  UpdateHarvestPayload,
} from '../types/harvest.types';

/**
 * Servicio de cosechas (MVP-401) sobre el cliente HTTP común (P-007).
 *
 * Cuarto recurso **operativo crítico** del producto y mismo contrato que los tres anteriores:
 * `PATCH` y `DELETE` viajan con `If-Match` y la versión vigente del registro (ADR-0005), de modo que
 * dos personas del mismo Workspace no puedan pisarse una corrección en silencio. Si la versión está
 * desfasada, la API responde `409 CONFLICT_VERSION_MISMATCH` y el `HttpError` llega con ese código
 * para que la vista pueda refrescar en vez de dejar al usuario sin salida.
 */
export function createHarvestService(http: HttpClient) {
  return {
    /** Cosechas del Workspace por fecha de negocio descendente (RN-033), con los kilos acumulados. */
    async listHarvests(filters?: HarvestFilters): Promise<HarvestListResponse> {
      return http.request<HarvestListResponse>('/api/v1/harvests', {
        query: {
          from: filters?.from,
          to: filters?.to,
          plot_id: filters?.plotId,
          season_id: filters?.seasonId,
          destination: filters?.destination,
        },
      });
    },

    /**
     * MVP-805 (RN-044) — Partidas vivas iguales a la que se está escribiendo: mismo terreno, misma
     * fecha y mismo producto. Alimenta un **aviso no bloqueante**, así que quien la llama trata el
     * fallo como «no se sabe» y no como un error de pantalla: no poder comprobarlo no puede impedir
     * registrar una cosecha.
     */
    async findDuplicates(query: HarvestDuplicateQuery): Promise<HarvestDuplicateListResponse> {
      return http.request<HarvestDuplicateListResponse>('/api/v1/harvests/duplicates', {
        query: {
          plot_id: query.plotId,
          date: query.date,
          product: query.product,
          exclude_id: query.excludeId,
        },
      });
    },

    /**
     * Una cosecha concreta. La usa el diario unificado para abrir el formulario de corrección: su
     * entrada es una proyección común de los cuatro tipos y no lleva todos los campos.
     */
    async getHarvest(harvestId: string): Promise<Harvest> {
      return http.request<Harvest>(`/api/v1/harvests/${harvestId}`);
    },

    async createHarvest(payload: CreateHarvestPayload): Promise<Harvest> {
      return http.request<Harvest>('/api/v1/harvests', { method: 'POST', body: payload });
    },

    /** Corrige una cosecha. `version` es la que el cliente cree vigente (ADR-0005). */
    async updateHarvest(
      harvestId: string,
      version: number,
      payload: UpdateHarvestPayload
    ): Promise<Harvest> {
      return http.request<Harvest>(`/api/v1/harvests/${harvestId}`, {
        method: 'PATCH',
        body: payload,
        headers: { 'If-Match': String(version) },
      });
    },

    /** Eliminación **lógica** (RN-037). La confirmación explícita la pide la UI antes de llamar. */
    async deleteHarvest(harvestId: string, version: number): Promise<void> {
      await http.request<void>(`/api/v1/harvests/${harvestId}`, {
        method: 'DELETE',
        headers: { 'If-Match': String(version) },
      });
    },
  };
}

export type HarvestService = ReturnType<typeof createHarvestService>;
