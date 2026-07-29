/**
 * Tipo de entrada del diario (MVP-305). Catálogo cerrado `diary_entry_type`.
 *
 * `cosecha` **no está todavía**: `HARVEST` no existe hasta MVP-004. RN-033 define el diario como la
 * mezcla de actividades, cosechas y compras/consumos, así que encenderla es alcance de MVP-401
 * (hallazgo `G-4`). La tarjeta está construida para que añadirla sea una entrada más en los mapas de
 * abajo.
 */
export type DiaryEntryType = 'actividad' | 'compra' | 'consumo';

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
}

export interface DiaryListResponse {
  data: DiaryEntry[];
  meta: {
    total: number;
    total_cost: number;
    activities: number;
    purchases: number;
    consumptions: number;
    consumptions_without_purchase: number;
  };
}

export interface DiaryFilters {
  from?: string;
  to?: string;
  plotId?: string;
  seasonId?: string;
  types?: DiaryEntryType[];
}

/** Cómo se pinta cada tipo en el muro. Añadir `cosecha` en MVP-401 es una entrada más aquí. */
export const DIARY_ENTRY_STYLES: Record<
  DiaryEntryType,
  { label: string; icon: string; badgeClass: string }
> = {
  actividad: { label: 'Labor', icon: 'content_cut', badgeClass: 'bg-[#4a5d23]' },
  compra: { label: 'Compra', icon: 'shopping_bag', badgeClass: 'bg-[#5a3811]' },
  consumo: { label: 'Consumo', icon: 'inventory_2', badgeClass: 'bg-[#7a6a1f]' },
};

/** Qué se le dice al usuario antes de eliminar cada tipo (RN-037: confirmación explícita). */
export const DIARY_ENTRY_NOUNS: Record<DiaryEntryType, string> = {
  actividad: 'la actividad',
  compra: 'la compra',
  consumo: 'el consumo',
};
