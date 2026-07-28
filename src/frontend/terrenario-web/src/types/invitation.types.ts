import type { Workspace } from './workspace.types';

/** Catálogo cerrado `invitation_channel`: los valores son vocabulario de dominio. */
export type InvitationChannel = 'email' | 'enlace';

/**
 * Catálogo cerrado `invitation_status`. La caducidad se deriva de `expires_at`. `anulada` (MVP-207)
 * la fija el Workspace emisor al retirar una invitación pendiente; `rechazada` la fija la persona
 * invitada al declinarla (MVP-107).
 */
export type InvitationStatus = 'pendiente' | 'aceptada' | 'rechazada' | 'anulada';

/**
 * Motivo por el que la cuenta autenticada no puede aceptar una invitación (MVP-107, R-C). Se
 * calcula en servidor y se muestra antes de aceptar, para no toparse con un error tras pulsar.
 */
export type InvitationViewerReason =
  | 'email_mismatch'
  | 'expired'
  | 'already_used'
  | 'already_rejected'
  | 'cancelled'
  | 'already_member';

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
  /** Aptitud de la cuenta autenticada para aceptar (MVP-107, R-C). */
  viewer: {
    can_accept: boolean;
    reason: InvitationViewerReason | null;
  };
}

/**
 * Invitación recibida por la cuenta autenticada (MVP-107, HU-3). Se identifica por `id` —nunca por
 * token— porque quien la recibe por email jamás tuvo el enlace en claro.
 */
export interface ReceivedInvitation {
  id: string;
  channel: InvitationChannel;
  workspace: Workspace;
  invited_by: string | null;
  expires_at: string;
  created_at: string;
}

export interface AcceptInvitationResponse {
  workspace: Workspace;
  access_token: string;
  expires_in: number;
  already_member: boolean;
}
