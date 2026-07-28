export interface Workspace {
  id: string;
  name: string;
}

/**
 * La creación devuelve un access token nuevo que ya lleva el Workspace como contexto
 * activo de la sesión (MVP-102, CA-2).
 */
export interface CreateWorkspaceResponse {
  workspace: Workspace;
  access_token: string;
  expires_in: number;
}

/** Catálogo cerrado `worker_member_status`: valores en español por ser vocabulario de dominio. */
export type WorkspaceMemberStatus = 'invitado' | 'activo' | 'revocado';

/**
 * Workspace disponible en el selector (MVP-104). `is_active` marca el que ejecuta las
 * operaciones ahora mismo; solo uno lo tiene a la vez.
 */
export interface WorkspaceMembership {
  id: string;
  name: string;
  role: string;
  status: WorkspaceMemberStatus;
  is_active: boolean;
  joined_at: string;
}

export interface WorkspaceListResponse {
  data: WorkspaceMembership[];
  meta: {
    total: number;
    active_workspace_id: string | null;
  };
}

/**
 * Cambiar de Workspace reemite la sesión situada en el destino (MVP-104, CA-2), igual que
 * el alta de Workspace: el contexto activo nunca viaja como parámetro de negocio.
 */
export interface SwitchWorkspaceResponse {
  workspace: Workspace;
  access_token: string;
  expires_in: number;
}
