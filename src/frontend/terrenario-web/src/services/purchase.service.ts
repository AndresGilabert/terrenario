import type { HttpClient } from './http-client';
import type {
  CreatePurchasePayload,
  ProductSuggestion,
  ProductSuggestionListResponse,
  Purchase,
  PurchaseFilters,
  PurchaseListResponse,
  UpdatePurchasePayload,
} from '../types/purchase.types';

/**
 * Servicio del libro de compras (MVP-303) sobre el cliente HTTP común (P-007). Segunda entidad
 * operativa crítica: `PATCH` y `DELETE` viajan con `If-Match` y la versión vigente (ADR-0005), igual
 * que las actividades.
 */
export function createPurchaseService(http: HttpClient) {
  return {
    /** Compras del Workspace por fecha de compra descendente, con el gasto acumulado de lo filtrado. */
    async listPurchases(filters?: PurchaseFilters): Promise<PurchaseListResponse> {
      return http.request<PurchaseListResponse>('/api/v1/purchases', {
        query: {
          product: filters?.product,
          season_id: filters?.seasonId,
          from: filters?.from,
          to: filters?.to,
        },
      });
    },

    /** Vocabulario de materiales del histórico para sugerir mientras se escribe (RN-031). */
    async listProductSuggestions(search?: string): Promise<ProductSuggestion[]> {
      const body = await http.request<ProductSuggestionListResponse>('/api/v1/purchases/products', {
        query: { search },
      });
      return body.data;
    },

    async createPurchase(payload: CreatePurchasePayload): Promise<Purchase> {
      return http.request<Purchase>('/api/v1/purchases', { method: 'POST', body: payload });
    },

    async updatePurchase(
      purchaseId: string,
      version: number,
      payload: UpdatePurchasePayload
    ): Promise<Purchase> {
      return http.request<Purchase>(`/api/v1/purchases/${purchaseId}`, {
        method: 'PATCH',
        body: payload,
        headers: { 'If-Match': String(version) },
      });
    },

    /** Eliminación **lógica** (RN-037). La confirmación explícita la pide la UI antes de llamar. */
    async deletePurchase(purchaseId: string, version: number): Promise<void> {
      await http.request<void>(`/api/v1/purchases/${purchaseId}`, {
        method: 'DELETE',
        headers: { 'If-Match': String(version) },
      });
    },
  };
}

export type PurchaseService = ReturnType<typeof createPurchaseService>;
