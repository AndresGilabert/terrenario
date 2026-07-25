import { API_BASE, readErrorBody } from './api.config';
import type {
  CreateWorkspaceResponse,
  SwitchWorkspaceResponse,
  Workspace,
  WorkspaceListResponse,
  WorkspaceMembership,
} from '../types/workspace.types';

export const workspaceService = {
  async createWorkspace(name: string, accessToken: string): Promise<CreateWorkspaceResponse> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ name }),
    });

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new WorkspaceServiceError(
        errorBody?.error?.code ?? 'WORKSPACE_CREATE_FAILED',
        errorBody?.error?.message ?? 'No se pudo crear el Workspace. Inténtalo de nuevo.'
      );
    }

    return response.json();
  },

  /** Devuelve `null` cuando el usuario todavía no tiene ningún Workspace. */
  async getActiveWorkspace(accessToken: string): Promise<Workspace | null> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces/active`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (response.status === 404) return null;

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new WorkspaceServiceError(
        errorBody?.error?.code ?? 'WORKSPACE_FETCH_FAILED',
        errorBody?.error?.message ?? 'No se pudo cargar tu Workspace.'
      );
    }

    return response.json();
  },

  /** MVP-104 — Workspaces a los que el usuario puede alternar (HU-1). */
  async listWorkspaces(accessToken: string): Promise<WorkspaceMembership[]> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new WorkspaceServiceError(
        errorBody?.error?.code ?? 'WORKSPACE_LIST_FAILED',
        errorBody?.error?.message ?? 'No se pudieron cargar tus Workspaces.'
      );
    }

    const body = (await response.json()) as WorkspaceListResponse;
    return body.data;
  },

  /** MVP-104 — Cambia el Workspace activo y devuelve la sesión reemitida (HU-2). */
  async switchWorkspace(
    workspaceId: string,
    accessToken: string
  ): Promise<SwitchWorkspaceResponse> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces/active`, {
      method: 'PUT',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ workspace_id: workspaceId }),
    });

    if (!response.ok) {
      const errorBody = await readErrorBody(response);
      throw new WorkspaceServiceError(
        errorBody?.error?.code ?? 'WORKSPACE_SWITCH_FAILED',
        errorBody?.error?.message ?? 'No se pudo cambiar de Workspace. Inténtalo de nuevo.'
      );
    }

    return response.json();
  },
};

export class WorkspaceServiceError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = 'WorkspaceServiceError';
    this.code = code;
  }
}
