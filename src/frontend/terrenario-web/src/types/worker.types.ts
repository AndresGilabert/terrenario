/**
 * Catálogo cerrado `worker_kind` (MVP-208): las dos clases de persona del maestro de responsables.
 * Se deriva de `user_account_id`; no es una columna propia.
 */
export type WorkerKind = 'member' | 'crew';

/**
 * Persona seleccionable como responsable de una labor (RN-002/RN-027). Desde MVP-208 el maestro es
 * **uno solo** y cubre las dos clases con el mismo espacio de identificadores:
 *
 * - `kind: 'member'` — miembro del Workspace. Su nombre llega de su cuenta de Google (RN-036) y su
 *   disponibilidad la gobierna la membresía: aquí solo se edita la tarifa horaria.
 * - `kind: 'crew'` — cuadrilla sin cuenta. Se da de alta, edita e inactiva en el maestro (MVP-204).
 *
 * `hourly_rate` es una tarifa de referencia opcional; no automatiza el coste (RN-003).
 */
export interface Worker {
  id: string;
  workspace_id: string;
  name: string;
  hourly_rate: number | null;
  is_active: boolean;
  kind: WorkerKind;
  /** Cuenta vinculada; `null` en la cuadrilla sin cuenta. */
  user_account_id: string | null;
}

/** Alta de trabajador de cuadrilla. Solo `name` es obligatorio (CA-2). */
export interface CreateWorkerPayload {
  name: string;
  hourly_rate?: number | null;
}

/**
 * Edición parcial: un campo ausente conserva su valor. En un responsable con cuenta solo se admite
 * `hourly_rate`; `name` e `is_active` responden 422 (MVP-208, CA-4).
 */
export interface UpdateWorkerPayload {
  name?: string;
  hourly_rate?: number | null;
  is_active?: boolean;
}

export interface WorkerListResponse {
  data: Worker[];
  meta: { total: number; members: number; crew: number };
}
