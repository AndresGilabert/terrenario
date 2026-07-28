import type { InvitationChannel } from './invitation.types';
import type { WorkspaceMemberStatus } from './workspace.types';

/**
 * Persona o acceso pendiente del Workspace en la vista de «Miembros y accesos» (MVP-204, HU-3). Es
 * una lista unificada: las membresías reales (`activo`/`revocado`) llegan como `kind: 'member'` y las
 * invitaciones pendientes como `kind: 'invitation'` con estado `invitado`. El estado `invitado` no es
 * una fila de membresía: se proyecta desde las invitaciones pendientes (decisión de diseño del spec).
 *
 * MVP-208 (CA-7): incluye los **dos canales**. Una invitación por `enlace` no tiene destinatario
 * (`email: null`), así que no es una persona, pero sí un acceso vivo que hay que poder retirar.
 */
export type WorkspacePersonKind = 'member' | 'invitation';

export interface WorkspacePerson {
  kind: WorkspacePersonKind;
  status: WorkspaceMemberStatus;
  /** Miembros: email de la cuenta. Invitaciones por enlace: `null` (no hay destinatario). */
  email: string | null;
  /** Miembros: nombre de la cuenta. Invitaciones: `null` (la persona puede no tener cuenta aún). */
  name: string | null;

  // Solo `kind === 'member'`
  user_id?: string;
  role?: string;
  joined_at?: string;
  is_self?: boolean;
  /** Señal del servidor: el miembro puede revocarse (activo y no propietario). CA-8 se valida en API. */
  can_revoke?: boolean;

  // Solo `kind === 'invitation'`
  invitation_id?: string;
  channel?: InvitationChannel;
  invited_at?: string;
  expires_at?: string;
  is_expired?: boolean;
}

export interface WorkspacePeopleResponse {
  data: WorkspacePerson[];
  meta: {
    total: number;
    active: number;
    invited: number;
    revoked: number;
  };
}

/** Resultado del reenvío de una invitación (MVP-204, HU-5/CA-6). */
export interface ResendInvitationResult {
  id: string;
  channel: InvitationChannel;
  /** `null` en el canal `enlace`: no hay destinatario al que escribir (MVP-208). */
  email: string | null;
  accept_url: string;
  expires_at: string;
  email_sent: boolean;
}
