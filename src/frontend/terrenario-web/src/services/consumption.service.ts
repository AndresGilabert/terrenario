import type { HttpClient } from './http-client';
import type {
  Consumption,
  ConsumptionFilters,
  ConsumptionListResponse,
  ImputePurchasePayload,
  RegisterConsumptionPayload,
  UpdateConsumptionPayload,
} from '../types/consumption.types';

/**
 * Servicio de consumos e imputaciones (MVP-304) sobre el cliente HTTP común (P-007).
 *
 * Dos altas para el mismo recurso, que es exactamente lo que dice el contrato: una cuelga de la
 * compra (`POST /purchases/{id}/consumptions`) y la otra no (`POST /consumptions`, RN-032). La
 * segunda es la que garantiza que la falta de compra **nunca** bloquee el registro.
 */
export function createConsumptionService(http: HttpClient) {
  return {
    /** Consumos del Workspace por fecha de negocio descendente (CA-4). */
    async listConsumptions(filters?: ConsumptionFilters): Promise<ConsumptionListResponse> {
      return http.request<ConsumptionListResponse>('/api/v1/consumptions', {
        query: {
          from: filters?.from,
          to: filters?.to,
          plot_id: filters?.plotId,
          season_id: filters?.seasonId,
          purchase_id: filters?.purchaseId,
          product: filters?.product,
        },
      });
    },

    /** Imputa una compra a un terreno con cantidad aproximada y coste proporcional (CA-1). */
    async imputePurchase(purchaseId: string, payload: ImputePurchasePayload): Promise<Consumption> {
      return http.request<Consumption>(`/api/v1/purchases/${purchaseId}/consumptions`, {
        method: 'POST',
        body: payload,
      });
    },

    /** Registra un consumo **sin compra previa**: coste 0 y aviso (CA-2, RN-032). */
    async registerConsumption(payload: RegisterConsumptionPayload): Promise<Consumption> {
      return http.request<Consumption>('/api/v1/consumptions', { method: 'POST', body: payload });
    },

    async updateConsumption(
      consumptionId: string,
      version: number,
      payload: UpdateConsumptionPayload
    ): Promise<Consumption> {
      return http.request<Consumption>(`/api/v1/consumptions/${consumptionId}`, {
        method: 'PATCH',
        body: payload,
        headers: { 'If-Match': String(version) },
      });
    },

    /** Eliminación **lógica** (RN-037). La confirmación explícita la pide la UI antes de llamar. */
    async deleteConsumption(consumptionId: string, version: number): Promise<void> {
      await http.request<void>(`/api/v1/consumptions/${consumptionId}`, {
        method: 'DELETE',
        headers: { 'If-Match': String(version) },
      });
    },
  };
}

export type ConsumptionService = ReturnType<typeof createConsumptionService>;
