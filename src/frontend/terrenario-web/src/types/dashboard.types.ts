/**
 * Dashboard del Workspace (MVP-403). Es **solo lectura** y sin refresco continuo: los datos se
 * recalculan al entrar a la pantalla o al recargar (RN-006).
 */

/**
 * Ámbito de lectura ya resuelto por el servidor (RN-008): sin filtros aplica la temporada activa y
 * todos los terrenos activos. Viaja en la respuesta para que la pantalla pueda explicar de qué son las
 * cifras y para posicionar los filtros sin adivinar el defecto.
 */
export interface DashboardScope {
  /** `null` ⇒ el Workspace no tiene temporada que mirar: hay que pedirla, no mostrar ceros. */
  season: {
    id: string;
    name: string;
    is_active: boolean;
    start_date: string;
    end_date: string | null;
  } | null;
  plot_ids: string[];
  plots: number;
}

/** Resumen de temporada (CA-1). */
export interface DashboardSummary {
  scope: DashboardScope;
  total_kg: number;
  /**
   * Litros de aceite «cuando exista dato»: declarados o derivados del rendimiento (RN-014). `null`
   * significa **desconocido**, que no es lo mismo que cero litros.
   */
  total_liters: number | null;
  /** Rendimiento medio en L/100kg (RN-013), ponderado por kilos. `null` si no hay dato de aceite. */
  average_yield: number | null;
  harvests: number;
  /** Partidas con dato de aceite: permite decir sobre cuántas se ha promediado. */
  harvests_with_oil_data: number;
}

/** Kg por destino (CA-2). Las claves salen de la taxonomía cerrada de RN-012, incluido `desconocido`. */
export interface DashboardKgByDestination {
  scope: DashboardScope;
  data: { destination: string; kg: number }[];
  /** Total calculado en servidor: así resumen y gráfico no pueden discrepar por un redondeo. */
  meta: { total_kg: number };
}

/** P-021 — Producción agregada por temporada, para las tarjetas del maestro de temporadas. */
export interface SeasonProductionResponse {
  data: { season_id: string; season_name: string; total_kg: number; harvests: number }[];
  meta: { total: number };
}

/** Filtros del dashboard. Todos opcionales: los defectos los pone el servidor (RN-008). */
export interface DashboardFilters {
  seasonId?: string;
  plotIds?: string[];
}
