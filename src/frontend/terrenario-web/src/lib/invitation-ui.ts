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

const MS_PER_DAY = 1000 * 60 * 60 * 24;

/** Medianoche local de una fecha: es la referencia con la que una persona cuenta los días. */
function startOfDay(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}

/**
 * Texto discreto de caducidad para las tarjetas de invitación de la bandeja/modal.
 *
 * Se cuenta en **días de calendario**, no en fracciones de día: «mañana» significa el día siguiente
 * en el calendario, no «dentro de más de 24 horas». Contar por fracciones hacía que una invitación
 * que vence hoy a las 18:00 dijera «Caduca mañana» y que «Caduca hoy» solo apareciera cuando ya
 * había caducado —momento en el que además era falso— (`MVP-999`, `P-065`).
 *
 * Una invitación ya caducada se rotula como tal. El servidor no las devuelve en la bandeja
 * (`ListReceivedInvitationsHandler` las descarta), pero puede vencer mientras la pantalla está
 * abierta, y ahí el texto tiene que decir la verdad.
 */
export function expiresLabel(expiresAt: string): string {
  const expiry = new Date(expiresAt);
  const now = new Date();

  if (expiry.getTime() <= now.getTime()) return 'Caducada';

  const days = Math.round((startOfDay(expiry) - startOfDay(now)) / MS_PER_DAY);

  if (days <= 0) return 'Caduca hoy';
  if (days === 1) return 'Caduca mañana';
  return `Caduca en ${days} días`;
}
