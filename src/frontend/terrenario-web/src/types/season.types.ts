/**
 * Temporada (campaña) del Workspace (MVP-201). En MVP hay una única temporada activa por Workspace
 * (RN-022). El estado se expone con los booleanos canónicos; el maestro completo llega con MVP-203.
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
}

/** Creación de la (primera) temporada activa del Workspace (MVP-201). */
export interface CreateSeasonPayload {
  name: string;
  start_date: string;
  end_date: string | null;
}
