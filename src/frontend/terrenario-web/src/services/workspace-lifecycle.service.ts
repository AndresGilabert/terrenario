import type { HttpClient } from './http-client';
import type { Workspace } from '../types/workspace.types';
import type {
  ClosedWorkspacesResponse,
  OwnershipObligationsResponse,
  ReactivationPreview,
  ReactivationRequestsResponse,
  WorkspaceClosureOptions,
  WorkspaceClosureResult,
} from '../types/workspace-lifecycle.types';

/**
 * Ciclo de vida del Workspace (MVP-206) sobre el cliente HTTP común. Renombrar, dar de baja y
 * traspasar actúan siempre sobre el **Workspace activo**: el contexto se resuelve en servidor y no
 * viaja como parámetro (RN-034), igual que en el resto de recursos con ámbito.
 */
export function createWorkspaceLifecycleService(http: HttpClient) {
  return {
    /** Renombra el Workspace activo (HU-1). Cualquier miembro activo; no reemite la sesión. */
    async rename(name: string): Promise<Workspace> {
      return http.request<Workspace>('/api/v1/workspaces/active', {
        method: 'PATCH',
        body: { name },
      });
    },

    /** Qué implica dar de baja el Workspace activo para quien pregunta (árbol de decisión). */
    async getClosureOptions(): Promise<WorkspaceClosureOptions> {
      return http.request<WorkspaceClosureOptions>('/api/v1/workspaces/active/closure');
    },

    /**
     * Da de baja el Workspace activo. Con copropietarios el Workspace sigue vivo y cambia de manos
     * (CA-5); siendo propietario único es una baja lógica con aviso al resto (CA-2/CA-6).
     */
    async close(): Promise<WorkspaceClosureResult> {
      return http.request<WorkspaceClosureResult>('/api/v1/workspaces/active/closure', {
        method: 'POST',
      });
    },

    /** Traspasa la propiedad a un miembro activo (CA-4). Quien traspasa se queda como miembro. */
    async transferOwnership(newOwnerUserId: string): Promise<WorkspaceClosureResult> {
      return http.request<WorkspaceClosureResult>('/api/v1/workspaces/active/transfer-ownership', {
        method: 'POST',
        body: { new_owner_user_id: newOwnerUserId },
      });
    },

    /** Workspaces de propiedad única que habría que resolver antes de cerrar la cuenta (CA-9). */
    async getOwnershipObligations(): Promise<OwnershipObligationsResponse> {
      return http.request<OwnershipObligationsResponse>('/api/v1/workspaces/ownership-obligations');
    },
  };
}

export type WorkspaceLifecycleService = ReturnType<typeof createWorkspaceLifecycleService>;

/**
 * Reactivación de un Workspace dado de baja (HU-5/HU-6). Estas rutas **no** exigen Workspace activo:
 * el Workspace en cuestión no resuelve contexto (CA-8) y puede ser el único que tuviera la persona.
 */
export function createReactivationService(http: HttpClient) {
  return {
    /** Lee el enlace sin consumirlo: informa antes de pulsar. */
    async preview(token: string): Promise<ReactivationPreview> {
      return http.request<ReactivationPreview>(
        `/api/v1/workspaces/reactivations/${encodeURIComponent(token)}`
      );
    },

    /** Consume el enlace (un solo uso, CA-10) y deja la solicitud a la espera de autorización. */
    async request(token: string): Promise<ReactivationPreview> {
      return http.request<ReactivationPreview>(
        `/api/v1/workspaces/reactivations/${encodeURIComponent(token)}/request`,
        { method: 'POST' }
      );
    },

    /** Solicitudes que espera resolver quien dio de baja los Workspaces (HU-6). */
    async listPendingAuthorizations(): Promise<ReactivationRequestsResponse> {
      return http.request<ReactivationRequestsResponse>('/api/v1/workspaces/reactivations');
    },

    /** Autoriza: el Workspace vuelve y la propiedad pasa al solicitante (CA-7). */
    async authorize(requestId: string): Promise<{ workspace: { id: string; name: string } }> {
      return http.request(`/api/v1/workspaces/reactivations/${requestId}/authorize`, {
        method: 'POST',
      });
    },

    async deny(requestId: string): Promise<void> {
      await http.request<void>(`/api/v1/workspaces/reactivations/${requestId}/deny`, {
        method: 'POST',
      });
    },

    /** Workspaces que dio de baja la propia cuenta: la baja lógica es reversible (CA-2). */
    async listClosed(): Promise<ClosedWorkspacesResponse> {
      return http.request<ClosedWorkspacesResponse>('/api/v1/workspaces/reactivations/closed');
    },

    /** Vuelve a levantar un Workspace que dio de baja la propia cuenta, sin pedirle permiso a nadie. */
    async reopen(workspaceId: string): Promise<Workspace> {
      return http.request<Workspace>(
        `/api/v1/workspaces/reactivations/closed/${workspaceId}/reopen`,
        { method: 'POST' }
      );
    },
  };
}

export type ReactivationService = ReturnType<typeof createReactivationService>;
