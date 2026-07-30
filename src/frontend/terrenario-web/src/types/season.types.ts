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
