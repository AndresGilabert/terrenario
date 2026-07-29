/**
 * Consumo de material en un terreno (MVP-304). Cubre los **dos** casos con la misma forma, porque son
 * el mismo hecho y solo cambia de dónde sale el coste:
 *
 * - **Imputación de una compra** (`purchase_id` informado): el producto, la temporada y el precio
 *   unitario se heredan de ella y el coste es proporcional a la cantidad.
 * - **Consumo sin compra previa** (`purchase_id: null`, RN-032): el coste es `0` porque se
 *   **desconoce**, no porque fuera gratis. `has_purchase: false` es la señal con la que la UI avisa.
 */
export interface Consumption {
  id: string;
  workspace_id: string;
  purchase_id: string | null;
  /** `false` ⇒ coste desconocido: se registró sin compra previa (RN-032, CA-2). */
  has_purchase: boolean;
  plot_id: string;
  plot_name: string;
  season_id: string;
  season_name: string;
  /** Fecha de negocio (`YYYY-MM-DD`). Es la que ordena el diario (RN-033, CA-4). */
  date: string;
  product: string;
  quantity: number;
  /** Precio unitario **congelado** al imputar; `0` sin compra. */
  unit_price: number;
  proportional_cost: number;
  /** RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada. */
  is_out_of_season_range: boolean;
  version: number;
  created_at: string;
  updated_at: string;
}

/** Imputación de una compra a un terreno. El producto y la temporada los pone la compra. */
export interface ImputePurchasePayload {
  date: string;
  plot_id: string;
  quantity: number;
}

/** Consumo sin compra previa: aquí sí hacen falta producto y temporada (RN-031, RN-021). */
export interface RegisterConsumptionPayload {
  date: string;
  plot_id: string;
  season_id: string;
  product: string;
  quantity: number;
}

/** Edición parcial de un consumo. El precio unitario no es editable (RN-032). */
export interface UpdateConsumptionPayload {
  date?: string;
  plot_id?: string;
  season_id?: string;
  product?: string;
  quantity?: number;
}

export interface ConsumptionListResponse {
  data: Consumption[];
  /** `without_purchase` mide el impacto en la calidad del dato (CA-3 de la épica). */
  meta: { total: number; total_cost: number; without_purchase: number };
}

export interface ConsumptionFilters {
  from?: string;
  to?: string;
  plotId?: string;
  seasonId?: string;
  purchaseId?: string;
}

/** No se puede repartir más material del que se compró (400). */
export const VALIDATION_CONSUMPTION_OVERFLOW = 'VALIDATION_CONSUMPTION_OVERFLOW';

/** La compra todavía tiene imputaciones vivas: hay que retirarlas antes de eliminarla (422). */
export const BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS = 'BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS';

export const CONSUMPTION_PRODUCT_MAX_LENGTH = 150;
