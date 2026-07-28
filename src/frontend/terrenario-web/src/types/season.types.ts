/**
 * Temporada (campaña) del Workspace (MVP-201 · maestro MVP-203). En MVP hay una única temporada
 * activa por Workspace (RN-022). Los estados `planificada/activa/cerrada` se derivan de los booleanos
 * canónicos; el backend los expone ya resueltos en `status` para las etiquetas y acciones de la UI.
 */
export interface Season {
  id: string;
  name: string;
  /** Fecha ISO `YYYY-MM-DD`. */
  start_date: string;
  /** Fecha ISO `YYYY-MM-DD`. Estimada y opcional. */
  end_date: string | null;
  is_active: boolean;
  is_closed: boolean;
  /** Estado derivado (planificada/activa/cerrada). */
  status: SeasonStatus;
}

/** Máquina de estados de la temporada (RN-024). Derivada de `is_active`/`is_closed` en el backend. */
export type SeasonStatus = 'planificada' | 'activa' | 'cerrada';

export const SEASON_STATUS_LABELS: Record<SeasonStatus, string> = {
  planificada: 'Planificada',
  activa: 'Activa',
  cerrada: 'Cerrada',
};

/**
 * Creación de una temporada del Workspace. La nueva pasa a ser la activa (MVP-203); la primera de un
 * Workspace simplemente nace activa (onboarding MVP-201).
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
