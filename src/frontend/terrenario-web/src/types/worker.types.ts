/**
 * Trabajador del maestro del Workspace (MVP-204). Cubre a los trabajadores **sin cuenta vinculada**
 * (cuadrilla, jornaleros). Los miembros del Workspace se exponen automáticamente como seleccionables
 * aparte (RN-027), desde la vista de personas. `hourly_rate` es una tarifa de referencia opcional; no
 * automatiza el coste (RN-003).
 */
export interface Worker {
  id: string;
  workspace_id: string;
  name: string;
  hourly_rate: number | null;
  is_active: boolean;
}

/** Alta de trabajador. Solo `name` es obligatorio (CA-2). */
export interface CreateWorkerPayload {
  name: string;
  hourly_rate?: number | null;
}

/** Edición de trabajador. `is_active` opcional (inactivación CA-3). */
export interface UpdateWorkerPayload extends CreateWorkerPayload {
  is_active?: boolean;
}

export interface WorkerListResponse {
  data: Worker[];
  meta: { total: number };
}
