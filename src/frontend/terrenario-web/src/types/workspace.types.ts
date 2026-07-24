export interface Workspace {
  id: string;
  nombre: string;
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
