import { API_BASE, readErrorBody } from './api.config';
import type { CreateWorkspaceResponse, Workspace } from '../types/workspace.types';

export const workspaceService = {
  async createWorkspace(nombre: string, accessToken: string): Promise<CreateWorkspaceResponse> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ nombre }),
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
    const response = await fetch(`${API_BASE}/api/v1/workspaces/activo`, {
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
};

export class WorkspaceServiceError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = 'WorkspaceServiceError';
    this.code = code;
  }
}
