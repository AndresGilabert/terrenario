import type { InvitationViewerReason } from '../types/invitation.types';

/**
 * Mensajes de aptitud del preview de invitación (MVP-107, R-C). Se muestran antes de aceptar para
 * anticipar el resultado y evitar el error tardío tras pulsar.
 */
const REASON_MESSAGES: Record<InvitationViewerReason, string> = {
  email_mismatch:
    'Esta invitación está dirigida a otra cuenta de correo. Entra con esa cuenta para aceptarla.',
  expired: 'Esta invitación ha caducado. Pide una nueva a quien te invitó.',
  already_used: 'Esta invitación ya se ha utilizado.',
  already_rejected: 'Esta invitación se ha rechazado y ya no está disponible.',
  cancelled: 'Quien te invitó ha anulado esta invitación. Pídele una nueva si sigues necesitando acceso.',
  already_member: 'Ya formas parte de este Workspace.',
};

export function viewerReasonMessage(reason: InvitationViewerReason | null): string | null {
  return reason ? REASON_MESSAGES[reason] : null;
}

/** Texto discreto de caducidad para las tarjetas de invitación de la bandeja/modal. */
export function expiresLabel(expiresAt: string): string {
  const expiry = new Date(expiresAt);
  const days = Math.ceil((expiry.getTime() - Date.now()) / (1000 * 60 * 60 * 24));

  if (days <= 0) return 'Caduca hoy';
  if (days === 1) return 'Caduca mañana';
  return `Caduca en ${days} días`;
}
