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
  /**
   * Kg por árbol del ámbito (MVP-405, CA-3, RN-010): Σkg / Σárboles de los terrenos que han producido
   * **y tienen** número de árboles. `null` si ninguno de ellos lo tiene (desconocido, no cero).
   */
  kg_per_tree: number | null;
  /** Árboles sobre los que se ha calculado `kg_per_tree` (denominador del KPI). */
  trees_counted: number;
  /** Terrenos con cosechas incluidos en `kg_per_tree` (tienen número de árboles). */
  plots_counted: number;
  /**
   * Terrenos con cosechas **excluidos** del KPI por no tener número de árboles (RN-010). `> 0` ⇒ el
   * widget avisa de que `kg_per_tree` es un dato incompleto.
   */
  plots_without_tree_count: number;
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

/** PRD-101 — Extremo de una recta de tendencia: fecha `YYYY-MM-DD` y rendimiento en L/100kg. */
export interface TrendPoint {
  date: string;
  value: number;
}

/** PRD-101 — Recta de regresión entregada como sus dos extremos: el cliente solo la dibuja. */
export interface TrendLine {
  from: TrendPoint;
  to: TrendPoint;
}

/**
 * Evolución de rendimiento (PRD-101). `data` tiene un punto por día cosechado en la unidad canónica
 * L/100kg (RN-013); `trend` es la recta de la temporada, `null` con menos de dos fechas distintas.
 *
 * `history` son las rectas de regresión de los mismos días de años anteriores (RN-015) y cubren todo
 * el periodo histórico de cosecha, también las semanas aún sin cosechar. Aparecen incluso sin
 * cosechas todavía en la campaña actual (`data` vacío). Cada recta es `null` mientras no haya
 * histórico suficiente.
 */
export interface DashboardYieldEvolution {
  scope: DashboardScope;
  data: { date: string; yield_l_per_100kg: number; kg: number }[];
  trend: TrendLine | null;
  history: {
    /** Recta de todo el histórico disponible, desde un año previo con dato. */
    general: TrendLine | null;
    /** Recta de los últimos 5 años; `null` si el histórico no llega 5 años atrás. */
    last_5_years: TrendLine | null;
    /** Recta de los últimos 10 años; `null` si el histórico no llega 10 años atrás. */
    last_10_years: TrendLine | null;
    prior_years_with_data: number;
  };
}

/** P-021 — Producción agregada por temporada, para las tarjetas del maestro de temporadas. */
export interface SeasonProductionResponse {
  data: { season_id: string; season_name: string; total_kg: number; harvests: number }[];
  meta: { total: number };
}

/**
 * MVP-707 — Lectura económica de la campaña (RN-009 ampliada): cuánto ha salido y cuánto ha entrado.
 *
 * `income` a `null` significa que **ninguna** partida del ámbito tiene precio, y la pantalla debe
 * decir «sin dato», no «0 €»: una campaña sin precios registrados no ha ingresado cero (CA-5).
 */
export interface DashboardEconomics {
  scope: DashboardScope;
  /** Labores + compras + consumos sin compra. Las imputaciones quedan fuera (`R-01` de MVP-399). */
  expense: number;
  income: number | null;
  harvests: number;
  /** Sobre cuántas partidas se ha sumado el ingreso. */
  harvests_with_price: number;
}

/** Filtros del dashboard. Todos opcionales: los defectos los pone el servidor (RN-008). */
export interface DashboardFilters {
  seasonId?: string;
  plotIds?: string[];
}
