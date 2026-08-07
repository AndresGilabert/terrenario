import type { SeasonScope } from './season.types';

/**
 * Compra de material del Workspace (MVP-303). El producto es **texto libre** (RN-031): no hay
 * catálogo cerrado, solo sugerencias aprendidas del histórico. Toda compra pertenece a una temporada
 * (RN-021, cierre de `P-050`).
 */
export interface Purchase {
  id: string;
  workspace_id: string;
  /** Fecha de negocio (`YYYY-MM-DD`). Es la que ordena el libro y el diario (RN-033). */
  purchase_date: string;
  season_id: string;
  season_name: string;
  product: string;
  total_quantity: number;
  total_cost: number;
  /** Derivado de coste/cantidad y persistido: es la base del coste proporcional de MVP-304. */
  unit_price: number;
  /** RN-023 — la fecha cae fuera del rango de la temporada. Aviso, nunca bloqueo. */
  is_out_of_season_range: boolean;
  /** MVP-304 — cantidad ya repartida entre terrenos. */
  imputed_quantity: number;
  /** MVP-304 — cantidad que queda por repartir; es el máximo de una imputación nueva (CA-1). */
  pending_quantity: number;
  /** Versión para el bloqueo optimista: viaja en `If-Match` (ADR-0005). */
  version: number;
  created_at: string;
  updated_at: string;
}

/** Alta de compra. El precio unitario no se envía: lo deriva el servidor. */
export interface CreatePurchasePayload {
  purchase_date: string;
  season_id: string;
  product: string;
  total_quantity: number;
  total_cost: number;
}

/** Edición parcial: un campo ausente conserva su valor. */
export type UpdatePurchasePayload = Partial<CreatePurchasePayload>;

export interface PurchaseListResponse {
  data: Purchase[];
  /** `total_cost` es el gasto acumulado de lo filtrado, calculado en servidor. */
  meta: { scope: SeasonScope; total: number; total_cost: number };
}

/** Filtros de `GET /api/v1/purchases`. */
export interface PurchaseFilters {
  product?: string;
  seasonId?: string;
  from?: string;
  to?: string;
}

/**
 * Material ya usado en el Workspace, con cuántas veces (RN-031, HU-2). **No es un catálogo**: no se
 * administra y siempre se puede escribir algo que no esté en la lista.
 */
export interface ProductSuggestion {
  product: string;
  times_used: number;
}

export interface ProductSuggestionListResponse {
  data: ProductSuggestion[];
  meta: { total: number };
}

export const PURCHASE_PRODUCT_MAX_LENGTH = 150;
