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
    /** Estado derivado (planificada/abierta/cerrada), MVP-209. */
    status: 'planificada' | 'abierta' | 'cerrada';
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

/** Kg por terreno (MVP-404, CA-1). El orden de `data` ya viene fijado por RN-011 (kg desc, alfabético). */
export interface DashboardKgByPlot {
  scope: DashboardScope;
  data: { plot_id: string; plot_name: string; kg: number }[];
  meta: { total_kg: number };
}

/**
 * Evolución de rendimiento (MVP-404, CA-2). La serie es el rendimiento del ámbito por periodo en la
 * unidad canónica L/100kg (RN-013); `history` es la comparativa histórica básica (RN-015).
 *
 * El histórico son **los mismos días de años anteriores** a los de las cosechas de la campaña activa
 * (`window`), no campañas agrupadas: así la parcela se compara con lo que ella misma rindió en esas
 * fechas otros años. Aparece incluso sin cosechas todavía en la campaña actual (solo la referencia,
 * `data` vacío). Cada media es `null` mientras no haya histórico suficiente.
 */
export interface DashboardYieldEvolution {
  scope: DashboardScope;
  granularity: 'month' | 'week';
  data: { period: string; yield_l_per_100kg: number; kg: number }[];
  history: {
    /** Promedio histórico de la ventana desde el primer año previo con dato. `null` si no hay ninguno. */
    average: number | null;
    /** Media de los últimos 5 años en la ventana; `null` si el histórico no llega 5 años atrás. */
    average_5_years: number | null;
    /** Media de los últimos 10 años en la ventana; `null` si el histórico no llega 10 años atrás. */
    average_10_years: number | null;
    prior_years_with_data: number;
    /** Tramo de calendario (`MM-DD`) sobre el que se compara. `null` si no hay ámbito resoluble. */
    window: { from: string; to: string } | null;
  };
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
