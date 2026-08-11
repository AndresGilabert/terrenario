import type { SeasonScope } from './season.types';

/**
 * Cosecha del Workspace (MVP-401). Es la materia prima del dashboard: convierte la recolección real
 * en kilos, destino y rendimiento comparables entre temporadas.
 *
 * El alcance del MVP se limita a producto, kilos, destino y **uno** entre rendimiento o litros
 * (RN-029). **MVP-707 lo matiza**: se admite un precio de venta por kilo opcional y su importe
 * derivado. Sigue sin haber molturación ni capa comercial.
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
  /**
   * Rendimiento **informado**, siempre en la unidad canónica L/100kg (RN-013), sea cual sea la unidad
   * en la que se escribió (RN-014). Excluyente con `liters` (RN-004).
   */
  yield: number | null;
  /** Litros de aceite obtenidos. Excluyente con `yield` (RN-004). */
  liters: number | null;
  /**
   * MVP-402 — Rendimiento en L/100kg **venga de donde venga**: el informado, o el derivado de los
   * litros y los kilos cuando lo que se declaró fueron litros (RN-014, tercer origen). Es lo que hace
   * que la exclusión de RN-004 no cueste información.
   */
  effective_yield: number | null;
  /** De dónde sale `effective_yield`: `informado`, `calculado` o `null` si no hay dato. */
  yield_source: YieldSource | null;
  /** Código del catálogo cerrado de destino (RN-012). */
  destination: string;
  /**
   * MVP-707 — Precio de venta por kilo. `null` significa **no se sabe**, no cero: una partida sin
   * precio no ha ingresado 0 €, es que todavía no se ha cerrado su venta o no se va a vender.
   */
  unit_price: number | null;
  /**
   * MVP-707 — Importe de la partida (`kgs × unit_price`), **derivado en servidor**. `null` si no hay
   * precio. No se persiste: guardarlo permitiría que divergiera de sus dos factores.
   */
  amount: number | null;
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
 * MVP-707 — Destinos que acaban en venta, y por tanto donde el precio por kilo se **ofrece** con
 * etiqueta propia. No es una restricción: el precio se admite en cualquier destino, porque quien vende
 * parte de una partida destinada a consumo propio también quiere apuntarlo.
 */
export const HARVEST_SALE_DESTINATIONS: readonly string[] = ['venta_aceituna', 'aceite_para_venta'];

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

/** MVP-402 — El rendimiento se declaró, o se dedujo de los litros obtenidos (RN-014). */
export type YieldSource = 'informado' | 'calculado';

/**
 * Cómo se informa el rendimiento en el formulario (RN-014). No es un campo del recurso: la API guarda
 * siempre la unidad canónica L/100kg o los litros (RN-004/RN-013), y convierte lo que haga falta.
 *
 * Los tres orígenes que admite RN-014, en el orden en que los ofrece el formulario:
 * `rendimiento` (L/100kg, la canónica), `rendimiento_graso` (kg de aceite por 100 kg, que es como lo
 * dan muchas almazaras) y `litros` (de los que se deriva el rendimiento).
 */
export type YieldInputMode = 'rendimiento' | 'rendimiento_graso' | 'litros' | 'ninguno';

/** Catálogo de unidades de entrada del rendimiento (RN-014). Lo persistido es siempre `l_100kg`. */
export const HARVEST_YIELD_UNITS = ['l_100kg', 'kg_100kg'] as const;

export type HarvestYieldUnit = (typeof HARVEST_YIELD_UNITS)[number];

/** Densidad por defecto del aceite de oliva (RN-016), para el equivalente que muestra el formulario. */
export const OIL_DENSITY_KG_PER_LITRE = 0.92;

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
  /**
   * MVP-402 — Unidad en la que va `yield` (RN-014). Ausente equivale a la canónica `l_100kg`; el
   * servidor convierte `kg_100kg` con la densidad de RN-016. No es un campo del recurso: lo que se
   * guarda y lo que se lee es siempre la canónica.
   */
  yield_unit?: HarvestYieldUnit | null;
  /**
   * MVP-707 — Precio de venta por kilo, opcional. `null` explícito **retira** el precio de una partida
   * que lo tenía. El importe no se envía: lo deriva el servidor.
   */
  unit_price?: number | null;
}

/** Edición parcial de cosecha: un campo ausente conserva su valor. */
export type UpdateHarvestPayload = Partial<CreateHarvestPayload>;

export interface HarvestListResponse {
  data: Harvest[];
  meta: {
    /** MVP-701 — Ámbito de temporada que ha aplicado el servidor (RN-008). */
    scope: SeasonScope;
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

/**
 * MVP-805 (RN-044, `RU-24`) — Lo que se pregunta para saber si ya hay una partida igual: **terreno,
 * fecha y producto**. `excludeId` es la propia partida al corregir.
 */
export interface HarvestDuplicateQuery {
  plotId: string;
  /** Fecha de negocio (`YYYY-MM-DD`). */
  date: string;
  product: string;
  excludeId?: string;
}

/**
 * Partida existente que coincide, con lo justo para **nombrarla** en el aviso: los kilos y el destino
 * son lo que permite distinguir de un vistazo si es la misma o una segunda de verdad.
 */
export interface HarvestDuplicate {
  id: string;
  kgs: number;
  destination: string;
}

export interface HarvestDuplicateListResponse {
  data: HarvestDuplicate[];
  meta: { total: number };
}
