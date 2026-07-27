import type { HttpClient } from './http-client';
import type {
  ResendInvitationResult,
  WorkspacePeopleResponse,
} from '../types/member.types';

/**
 * Servicio de administración de personas del Workspace (MVP-204, HU-3/HU-4/HU-5) sobre el cliente
 * HTTP común. Cubre el listado unificado, la revocación de acceso (CA-7/CA-8) y el reenvío de
 * invitaciones (CA-6). El scope de Workspace se resuelve en servidor (RN-034).
 */
export function createMemberService(http: HttpClient) {
  return {
    /** Lista unificada de personas del Workspace con su estado (activo/invitado/revocado). */
    async listPeople(): Promise<WorkspacePeopleResponse> {
      return http.request<WorkspacePeopleResponse>('/api/v1/workspace-members');
    },

    /** Retira el acceso de un miembro activo (CA-7). El backend impide dejar el Workspace vacío (CA-8). */
    async revokeMember(userId: string): Promise<void> {
      await http.request<void>(`/api/v1/workspace-members/${userId}/revoke`, { method: 'POST' });
    },

    /**
     * Reenvía una invitación por email pendiente (CA-6): token nuevo y caducidad renovada.
     * `deliverEmail` distingue reenviar por email (reenvía el correo) de por enlace (solo devuelve el
     * nuevo `accept_url` para compartirlo por otro medio).
     */
    async resendInvitation(invitationId: string, deliverEmail: boolean): Promise<ResendInvitationResult> {
      return http.request<ResendInvitationResult>(
        `/api/v1/workspaces/invitations/${invitationId}/resend`,
        { method: 'POST', body: { deliver_email: deliverEmail } }
      );
    },
  };
}

export type MemberService = ReturnType<typeof createMemberService>;
