/**
 * Temporada (campaña) del Workspace (MVP-201 · maestro MVP-203 · modelo MVP-209).
 *
 * MVP-209 separó dos ejes que antes fundía `is_active`:
 * - `status` (informativo): en qué punto de su vida está la campaña —planificada/abierta/cerrada—,
 *   derivado de `is_closed` y de la fecha de inicio frente a hoy, independiente de la de trabajo.
 * - `is_working`: si es la temporada de **trabajo del usuario** que consulta (sobre la que registra por
 *   defecto). Es por usuario, no por Workspace.
 */
export interface Season {
  id: string;
  name: string;
  /** Fecha ISO `YYYY-MM-DD`. */
  start_date: string;
  /** Fecha ISO `YYYY-MM-DD`. Estimada y opcional. */
  end_date: string | null;
  is_closed: boolean;
  /** MVP-209 — la temporada de trabajo de este usuario (antes `is_active`, que era por Workspace). */
  is_working: boolean;
  /** Estado derivado informativo (planificada/abierta/cerrada). */
  status: SeasonStatus;
  /**
   * MVP-806 (CA-2) — Cuántos registros la referencian. `null` significa **«no consultado»**, no
   * «ninguno»: solo lo trae el listado. Es lo que decide si se ofrece «Eliminar».
   */
  usage_count: number | null;
}

/** Estado informativo de la temporada (MVP-209). Derivado de `is_closed` + fechas en el backend. */
export type SeasonStatus = 'planificada' | 'abierta' | 'cerrada';

export const SEASON_STATUS_LABELS: Record<SeasonStatus, string> = {
  planificada: 'Planificada',
  abierta: 'Abierta',
  cerrada: 'Cerrada',
};

/**
 * Creación de una temporada del Workspace. Desde MVP-209 la nueva pasa a ser la temporada de **trabajo
 * del creador** (por usuario), sin desbancar a nadie.
 */
export interface CreateSeasonPayload {
  name: string;
  start_date: string;
  end_date: string | null;
}

/** Edición de una temporada: nombre, fechas y cierre/reapertura (`is_closed`). Campos parciales. */
export interface UpdateSeasonPayload {
  name?: string;
  start_date?: string;
  end_date?: string | null;
  is_closed?: boolean;
}

export interface SeasonListResponse {
  data: Season[];
  meta: { total: number };
}

/**
 * MVP-701 — Ámbito de temporada que el **servidor** ha aplicado a una lectura operativa (RN-008).
 *
 * Viaja en `meta.scope` del diario, las cosechas, las compras y los consumos por el mismo motivo por
 * el que ya viajaba en el dashboard: si el defecto lo resolviera el cliente, la regla viviría en dos
 * sitios y volvería a divergir, que es exactamente lo que produjo `P-082` —dos pantallas dando
 * totales distintos de la misma campaña—.
 */
export interface SeasonScope {
  /** Temporada aplicada; `null` si se está viendo el histórico completo o no hay ninguna. */
  season: SeasonScopeSeason | null;
  /** Se está viendo el histórico completo, por elección explícita o porque no hay de trabajo. */
  all_seasons: boolean;
}

export interface SeasonScopeSeason {
  id: string;
  name: string;
  status: SeasonStatus;
  start_date: string;
  end_date: string | null;
}

/**
 * MVP-701 — Valor reservado de `season_id` que pide el histórico completo. Sin él no se podría
 * distinguir «no he elegido, pon el defecto» de «quiero verlo todo»: la ausencia del parámetro ya
 * significa lo primero.
 */
export const ALL_SEASONS = 'all';
