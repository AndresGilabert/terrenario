import type { Workspace } from './workspace.types';

/**
 * MVP-206 — Casos del árbol de decisión de la baja, resueltos en servidor para que la UI plantee la
 * pregunta correcta y no reimplemente la regla de propiedad.
 *
 * - `auto_transfer`: hay copropietarios; la baja reasigna el Workspace y quien la pide sale (CA-5).
 * - `choose`: propietario único con más miembros; hay que elegir traspasar o dar de baja (CA-3).
 * - `only_delete`: propietario único sin nadie más; solo cabe la baja lógica.
 * - `not_owner`: quien mira no es propietario y no puede dar de baja ni traspasar.
 */
export type WorkspaceClosureMode = 'auto_transfer' | 'choose' | 'only_delete' | 'not_owner';

/** Miembro activo al que se puede traspasar la propiedad (CA-4). */
export interface OwnershipCandidate {
  user_id: string;
  name: string;
  email: string;
  role: string;
}

export interface WorkspaceClosureOptions {
  workspace: Workspace;
  is_owner: boolean;
  mode: WorkspaceClosureMode;
  active_owners: number;
  /** Solo en `auto_transfer`: copropietario que heredaría el Workspace. */
  successor_name: string | null;
  candidates: OwnershipCandidate[];
}

export type WorkspaceClosureOutcome = 'transferred' | 'deleted';

export interface WorkspaceClosureResult {
  outcome: WorkspaceClosureOutcome;
  workspace: Workspace;
  new_owner_name: string | null;
  /** Miembros avisados por email de la baja, con su enlace de reactivación (CA-6). */
  notified_members: number;
  emails_sent: number;
}

/** Workspace del que la cuenta es propietaria única y que bloquearía su baja (CA-9). */
export interface OwnershipObligation {
  workspace_id: string;
  name: string;
  other_active_members: number;
  can_transfer: boolean;
}

export interface OwnershipObligationsResponse {
  data: OwnershipObligation[];
  meta: { total: number; is_clear: boolean };
}

/** Estados del catálogo `reactivation_request_status` (MVP-206). */
export type ReactivationStatus = 'pendiente' | 'solicitada' | 'autorizada' | 'denegada' | 'cerrada';

/** Lo que ve quien abre el enlace de reactivación antes de decidir (HU-5). */
export interface ReactivationPreview {
  id: string;
  workspace: Workspace;
  closed_by: string | null;
  status: ReactivationStatus;
  expires_at: string;
  is_expired: boolean;
  can_request: boolean;
}

/** Solicitud pendiente de autorizar por quien dio de baja el Workspace (HU-6). */
export interface ReactivationRequest {
  id: string;
  workspace: Workspace;
  requested_by: { user_id: string; name: string; email: string };
  requested_at: string;
  expires_at: string;
}

export interface ReactivationRequestsResponse {
  data: ReactivationRequest[];
  meta: { total: number };
}

/** Workspace dado de baja por la propia cuenta, que puede volver a levantar sin pedir permiso. */
export interface ClosedWorkspace {
  id: string;
  name: string;
  closed_at: string;
}

export interface ClosedWorkspacesResponse {
  data: ClosedWorkspace[];
  meta: { total: number };
}
