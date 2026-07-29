/**
 * Cosecha del Workspace (MVP-401). Es la materia prima del dashboard: convierte la recolección real
 * en kilos, destino y rendimiento comparables entre temporadas.
 *
 * El alcance del MVP se limita a producto, kilos, destino y **uno** entre rendimiento o litros
 * (RN-029): aquí no hay precio, molturación ni balance.
 */
export interface Harvest {
  id: string;
  workspace_id: string;
  /** Fecha de negocio (`YYYY-MM-DD`). Es la que ordena el diario (RN-033), no la de captura. */
  date: string;
  plot_id: string;
  plot_name: string;
  season_id: string;
  season_name: string;
  /** Código del catálogo global fijo de producto (RN-030). */
  product: string;
  kgs: number;
  /** Rendimiento en L/100kg (RN-013). Excluyente con `liters` (RN-004). */
  yield: number | null;
  /** Litros de aceite obtenidos. Excluyente con `yield` (RN-004). */
  liters: number | null;
  /** Código del catálogo cerrado de destino (RN-012). */
  destination: string;
  /** RN-023 — la fecha cae fuera del rango de la temporada. Aviso, nunca bloqueo. */
  is_out_of_season_range: boolean;
  /** Versión para el bloqueo optimista: viaja en `If-Match` al editar o eliminar (ADR-0005). */
  version: number;
  created_at: string;
  updated_at: string;
}

/**
 * Catálogo global fijo de productos de cosecha (RN-030). Hoy tiene un solo valor: el MVP está ligado
 * al **olivar**, y ni el registro ni el dashboard distinguen variedades.
 *
 * Decisión de producto (2026-07-29): la **variedad** pertenece al terreno, no a la cosecha, y el
 * **producto** debería vivir a nivel de Workspace para modular el cálculo de rendimiento. Las dos
 * cosas son ampliaciones posteriores y quedan registradas en `MVP-999`; mientras tanto el campo
 * existe —RN-030 lo exige— pero no obliga a elegir.
 */
export const HARVEST_PRODUCTS = ['aceituna_olivar'] as const;

export type HarvestProduct = (typeof HARVEST_PRODUCTS)[number];

export const HARVEST_PRODUCT_LABELS: Record<string, string> = {
  aceituna_olivar: 'Aceituna de olivar',
};

/** Etiqueta legible de un producto; si llegara un código desconocido se muestra tal cual. */
export const harvestProductLabel = (product: string): string =>
  HARVEST_PRODUCT_LABELS[product] ?? product;

/** Catálogo cerrado `harvest_destination` (RN-012). `desconocido` es un valor válido, no un hueco. */
export const HARVEST_DESTINATIONS = [
  'venta_aceituna',
  'aceite_para_venta',
  'aceite_personal',
  'desconocido',
] as const;

export type HarvestDestination = (typeof HARVEST_DESTINATIONS)[number];

/**
 * Etiquetas visuales del destino. `desconocido` se rotula «Sin destino», que es el alias que RN-012
 * autoriza: el canon en base de datos no cambia.
 */
export const HARVEST_DESTINATION_LABELS: Record<string, string> = {
  venta_aceituna: 'Venta de aceituna',
  aceite_para_venta: 'Aceite para venta',
  aceite_personal: 'Aceite personal',
  desconocido: 'Sin destino',
};

export const harvestDestinationLabel = (destination: string): string =>
  HARVEST_DESTINATION_LABELS[destination] ?? destination;

/**
 * Cómo se informa el rendimiento en el formulario. No es un campo del recurso: la API guarda siempre
 * la unidad canónica L/100kg o los litros (RN-004/RN-013). Las **entradas equivalentes** de RN-014
 * (kg de aceite por 100 kg) son alcance de `MVP-402`.
 */
export type YieldInputMode = 'rendimiento' | 'litros' | 'ninguno';

/** Alta de cosecha. `yield` y `liters` son opcionales y excluyentes (RN-004). */
export interface CreateHarvestPayload {
  date: string;
  plot_id: string;
  season_id: string;
  product: string;
  kgs: number;
  destination: string;
  yield?: number | null;
  liters?: number | null;
}

/** Edición parcial de cosecha: un campo ausente conserva su valor. */
export type UpdateHarvestPayload = Partial<CreateHarvestPayload>;

export interface HarvestListResponse {
  data: Harvest[];
  meta: {
    total: number;
    /** Kilos acumulados de lo filtrado, calculados en servidor. */
    total_kg: number;
  };
}

/** Filtros de `GET /api/v1/harvests`. */
export interface HarvestFilters {
  from?: string;
  to?: string;
  plotId?: string;
  seasonId?: string;
  destination?: string;
}

export const HARVEST_KGS_MAX = 99_999_999.99;
/** No puede salir más aceite que fruto: por encima siempre es un error de tecleo. */
export const HARVEST_YIELD_MAX = 100;
