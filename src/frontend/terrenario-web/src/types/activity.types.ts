/**
 * Actividad del diario del Workspace (MVP-301). Es la unidad de captura más frecuente del MVP: qué
 * se hizo, quién, cuánto duró, cuánto costó y dónde.
 *
 * La respuesta trae ya resueltos los nombres de los maestros y dos campos derivados que evitan que
 * cada pantalla los recalcule: `task` (venga del catálogo o del texto libre, RN-025) y
 * `is_out_of_season_range` (aviso no bloqueante de RN-023).
 */
export interface Activity {
  id: string;
  workspace_id: string;
  /** Fecha de negocio (`YYYY-MM-DD`). Es la que ordena el diario (RN-033), no la de captura. */
  date: string;
  plot_id: string;
  plot_name: string;
  season_id: string;
  season_name: string;
  worker_id: string;
  worker_name: string;
  /** Tarea del catálogo, si se eligió de él (RN-025). */
  task_id: string | null;
  task_name: string | null;
  /** Tarea escrita al vuelo, si no venía del catálogo (RN-025). */
  task_text: string | null;
  /** Texto de la tarea venga de donde venga. */
  task: string;
  hours: number;
  manual_cost: number;
  description: string | null;
  /** RN-023 — la fecha cae fuera del rango de la temporada. Aviso, nunca bloqueo. */
  is_out_of_season_range: boolean;
  /** Versión para el bloqueo optimista: viaja en `If-Match` al editar o eliminar (ADR-0005). */
  version: number;
  created_at: string;
  updated_at: string;
  /**
   * MVP-302 — Qué pasó en el catálogo cuando se pidió guardar la tarea escrita a mano. `null` en las
   * lecturas y cuando no se pidió.
   */
  task_catalog_outcome: TaskCatalogOutcome | null;
}

/**
 * Resultado de guardar una tarea libre en el catálogo (MVP-302). No es lo mismo haber creado una
 * tarea que haber reutilizado —o reactivado— una que ya existía, y la UI lo dice tal cual.
 */
export type TaskCatalogOutcome = 'created' | 'reused' | 'reactivated';

export const TASK_CATALOG_OUTCOME_MESSAGES: Record<TaskCatalogOutcome, (name: string) => string> = {
  created: (name) => `«${name}» se ha añadido a tu catálogo de tareas.`,
  reused: (name) => `«${name}» ya estaba en tu catálogo: se ha reutilizado esa tarea.`,
  reactivated: (name) => `«${name}» estaba inactivada en tu catálogo y se ha vuelto a activar.`,
};

/** Alta de actividad. `task_id` y `task_text` son excluyentes y al menos uno es obligatorio (RN-025). */
export interface CreateActivityPayload {
  date: string;
  plot_id: string;
  season_id: string;
  worker_id: string;
  task_id?: string | null;
  task_text?: string | null;
  hours: number;
  manual_cost: number;
  description?: string | null;
  /**
   * MVP-302 — Guardar además `task_text` en el catálogo del Workspace (RN-026). Si el nombre ya
   * existe se reutiliza, y si estaba inactivada se reactiva: nunca crea una segunda tarea igual.
   */
  save_task_to_catalog?: boolean;
}

/** Edición parcial de actividad: un campo ausente conserva su valor. */
export type UpdateActivityPayload = Partial<CreateActivityPayload>;

export interface ActivityListResponse {
  data: Activity[];
  meta: { total: number };
}

/** Filtros de `GET /api/v1/activities`. */
export interface ActivityFilters {
  from?: string;
  to?: string;
  plotId?: string;
  seasonId?: string;
  workerId?: string;
}

/** Cota del texto libre de tarea; coincide con la del catálogo para que siempre se pueda guardar. */
export const ACTIVITY_TASK_TEXT_MAX_LENGTH = 120;
export const ACTIVITY_DESCRIPTION_MAX_LENGTH = 500;

/** Código del contrato para la colisión de versión (ADR-0005). */
export const CONFLICT_VERSION_MISMATCH = 'CONFLICT_VERSION_MISMATCH';
