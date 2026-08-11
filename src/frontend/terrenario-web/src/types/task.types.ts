/**
 * Tarea del catálogo del Workspace (MVP-205, RN-026). Cada Workspace mantiene el suyo, arranca
 * vacío y es editable por cualquier miembro. Las tareas con histórico se inactivan (`is_active`),
 * nunca se borran (CA-3).
 */
export interface WorkTask {
  id: string;
  workspace_id: string;
  name: string;
  is_active: boolean;
  /**
   * MVP-806 (CA-2) — Cuántos registros la referencian. `null` significa **«no consultado»**, no
   * «ninguno»: solo lo trae el listado. Es lo que decide si se ofrece «Eliminar».
   */
  usage_count: number | null;
}

/** Alta de tarea. Solo `name` es obligatorio (CA-2). */
export interface CreateTaskPayload {
  name: string;
  is_active?: boolean;
}

/** Edición parcial de tarea: renombrado y/o cambio de estado (inactivación CA-3). */
export interface UpdateTaskPayload {
  name?: string;
  is_active?: boolean;
}

export interface TaskListResponse {
  data: WorkTask[];
  meta: { total: number };
}

/** Nombre repetido en el catálogo del Workspace, ignorando mayúsculas (409). */
export const CONFLICT_TASK_NAME_DUPLICATE = 'CONFLICT_TASK_NAME_DUPLICATE';

/** Longitud máxima del nombre, alineada con el dominio (`TaskItem.NameMaxLength`). */
export const TASK_NAME_MAX_LENGTH = 120;
