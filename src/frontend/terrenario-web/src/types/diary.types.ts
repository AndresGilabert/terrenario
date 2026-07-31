/**
 * Tipo de entrada del diario (MVP-305). Catálogo cerrado `diary_entry_type`.
 *
 * `cosecha` la enciende **MVP-401**, que es quien crea `HARVEST`: RN-033 define el diario como la
 * mezcla de actividades, cosechas y compras/consumos, así que hasta entonces la vista principal estaba
 * incompleta por construcción (hallazgo `G-4`). Con los cuatro valores vivos, RN-033 queda cumplida
 * entera.
 */
export type DiaryEntryType = 'actividad' | 'compra' | 'consumo' | 'cosecha';

/**
 * Entrada del diario cronológico unificado. Es una **vista de lectura**: no hay entidad «entrada de
 * diario», sino la proyección común de las tres entidades operativas. Los campos opcionales los
 * rellena cada tipo si le aplican.
 *
 * `version` viaja porque eliminar desde el diario exige `If-Match` (ADR-0005): sin ella habría que
 * abrir el registro solo para poder borrarlo.
 */
export interface DiaryEntry {
  type: DiaryEntryType;
  id: string;
  /** Fecha de negocio (`YYYY-MM-DD`): la que ordena el diario (RN-033). */
  date: string;
  title: string;
  description: string | null;
  plot_id: string | null;
  plot_name: string | null;
  season_id: string;
  season_name: string;
  cost: number;
  version: number;
  is_out_of_season_range: boolean;
  created_at: string;
  /** Solo en actividades. */
  worker_name: string | null;
  hours: number | null;
  /**
   * Solo en actividades: tarea del catálogo, o `null` si se escribió a mano. Es lo que permite
   * ofrecer guardarla en el catálogo (MVP-302) solo cuando tiene sentido.
   */
  task_id: string | null;
  /** Solo en compras y consumos. */
  quantity: number | null;
  /** Solo en consumos: `false` ⇒ el coste es desconocido, no cero (RN-032). */
  has_purchase: boolean | null;
  /**
   * Solo en cosechas (MVP-401): kilos recolectados. Van aparte de `quantity` porque no son la misma
   * magnitud —allí es material comprado o consumido, sin unidad fija— y la tarjeta las rotula distinto.
   */
  kgs: number | null;
  /** Solo en cosechas: destino de lo recolectado (RN-012). */
  destination: string | null;
  /**
   * Solo en cosechas (MVP-402): rendimiento en la unidad canónica L/100kg (RN-013), sea declarado o
   * derivado de los litros obtenidos (RN-014). `null` cuando la partida no tiene dato de aceite.
   */
  yield: number | null;
}

export interface DiaryListResponse {
  data: DiaryEntry[];
  meta: {
    /**
     * MVP-506 — Entradas del diario **filtrado completo**, no de la página: es lo que permite saber
     * cuántas páginas hay. El resto de contadores e importes también son del conjunto, porque son la
     * cabecera del muro y cambiarían en cada avance si contaran solo lo visible.
     */
    total: number;
    page: number;
    limit: number;
    /**
     * Gasto real de lo que se está viendo: labores + compras + consumos **sin compra**. Las
     * imputaciones quedan fuera porque reparten dinero que la compra ya aportó (MVP-399, `R-01`).
     */
    total_cost: number;
    /** Lo repartido por terrenos: desglose de `total_cost`, no gasto añadido. */
    imputed_cost: number;
    activities: number;
    purchases: number;
    consumptions: number;
    consumptions_without_purchase: number;
    /** MVP-401 — cosechas de lo filtrado. */
    harvests: number;
    /** MVP-401 — kilos recolectados: la cosecha no aporta gasto (RN-029), así que se resume por kilos. */
    total_kg: number;
  };
}

export interface DiaryFilters {
  from?: string;
  to?: string;
  plotId?: string;
  seasonId?: string;
  types?: DiaryEntryType[];
  /**
   * MVP-506 (`P-056`) — Responsable de la labor. Solo las actividades lo tienen, así que filtrar por
   * él deja fuera compras, consumos y cosechas por definición, igual que filtrar por terreno deja
   * fuera las compras.
   */
  workerId?: string;
  /** MVP-506 (`P-052`) — Búsqueda por texto, resuelta en servidor sobre el diario completo. */
  search?: string;
  page?: number;
  limit?: number;
}

/** Tamaño de página del diario. Coincide con el defecto del servidor (`contratos-api.md`). */
export const DIARY_PAGE_SIZE = 20;

/** Cómo se pinta cada tipo en el muro. La cosecha (MVP-401) fue una entrada más aquí. */
export const DIARY_ENTRY_STYLES: Record<
  DiaryEntryType,
  { label: string; icon: string; badgeClass: string }
> = {
  actividad: { label: 'Labor', icon: 'content_cut', badgeClass: 'bg-[#4a5d23]' },
  compra: { label: 'Compra', icon: 'shopping_bag', badgeClass: 'bg-[#5a3811]' },
  consumo: { label: 'Consumo', icon: 'inventory_2', badgeClass: 'bg-[#7a6a1f]' },
  cosecha: { label: 'Cosecha', icon: 'agriculture', badgeClass: 'bg-[#33450d]' },
};

/** Qué se le dice al usuario antes de eliminar cada tipo (RN-037: confirmación explícita). */
export const DIARY_ENTRY_NOUNS: Record<DiaryEntryType, string> = {
  actividad: 'la actividad',
  compra: 'la compra',
  consumo: 'el consumo',
  cosecha: 'la cosecha',
};
