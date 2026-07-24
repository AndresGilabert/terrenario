import type { Workspace } from './workspace.types';

/** Catálogo cerrado `invitation_channel`: los valores son vocabulario de dominio. */
export type InvitationChannel = 'email' | 'enlace';

/** Catálogo cerrado `invitation_status`. La caducidad se deriva de `expires_at`. */
export type InvitationStatus = 'pendiente' | 'aceptada';

/**
 * `accept_url` solo llega en la respuesta de creación (MVP-103): el backend guarda únicamente
 * el hash del token, así que el enlace no puede recuperarse más tarde.
 */
export interface CreatedInvitation {
  id: string;
  channel: InvitationChannel;
  email: string | null;
  status: InvitationStatus;
  accept_url: string;
  expires_at: string;
  email_sent: boolean;
}

export interface PendingInvitation {
  id: string;
  channel: InvitationChannel;
  email: string | null;
  status: InvitationStatus;
  expires_at: string;
  created_at: string;
}

export interface InvitationPreview {
  id: string;
  channel: InvitationChannel;
  status: InvitationStatus;
  workspace: Workspace;
  invited_by: string | null;
  expires_at: string;
  is_expired: boolean;
}

export interface AcceptInvitationResponse {
  workspace: Workspace;
  access_token: string;
  expires_in: number;
  already_member: boolean;
}
