import { API_BASE, readErrorBody } from './api.config';
import type {
  AcceptInvitationResponse,
  CreatedInvitation,
  InvitationChannel,
  InvitationPreview,
  PendingInvitation,
} from '../types/invitation.types';

const MESSAGES: Record<string, string> = {
  INVITATION_NOT_FOUND: 'Esta invitación no existe o ya no es válida.',
  BUSINESS_RULE_INVITATION_EXPIRED: 'Esta invitación ha caducado. Pide una nueva a quien te invitó.',
  BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED: 'Esta invitación ya se ha utilizado.',
  BUSINESS_RULE_INVITATION_ALREADY_MEMBER: 'Esa persona ya forma parte de este Workspace.',
  AUTH_INVITATION_EMAIL_MISMATCH: 'Esta invitación está dirigida a otra cuenta de correo.',
};

export const invitationService = {
  async createInvitation(
    channel: InvitationChannel,
    email: string | null,
    accessToken: string
  ): Promise<CreatedInvitation> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces/invitations`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ channel, email }),
    });

    return handle(response, 'No se pudo crear la invitación. Inténtalo de nuevo.');
  },

  async listPendingInvitations(accessToken: string): Promise<PendingInvitation[]> {
    const response = await fetch(`${API_BASE}/api/v1/workspaces/invitations`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    const body = await handle<{ data: PendingInvitation[] }>(
      response,
      'No se pudieron cargar las invitaciones pendientes.'
    );

    return body.data;
  },

  async getInvitation(token: string, accessToken: string): Promise<InvitationPreview> {
    const response = await fetch(`${API_BASE}/api/v1/invitations/${encodeURIComponent(token)}`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    return handle(response, 'No se pudo cargar la invitación.');
  },

  async acceptInvitation(token: string, accessToken: string): Promise<AcceptInvitationResponse> {
    const response = await fetch(
      `${API_BASE}/api/v1/invitations/${encodeURIComponent(token)}/accept`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { Authorization: `Bearer ${accessToken}` },
      }
    );

    return handle(response, 'No se pudo aceptar la invitación. Inténtalo de nuevo.');
  },
};

async function handle<T>(response: Response, fallbackMessage: string): Promise<T> {
  if (response.ok) return response.json() as Promise<T>;

  const errorBody = await readErrorBody(response);
  const code = errorBody?.error?.code ?? 'INVITATION_REQUEST_FAILED';

  throw new InvitationServiceError(
    code,
    MESSAGES[code] ?? errorBody?.error?.message ?? fallbackMessage
  );
}

export class InvitationServiceError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = 'InvitationServiceError';
    this.code = code;
  }
}
