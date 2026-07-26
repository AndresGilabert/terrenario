import React from 'react';
import type { ReceivedInvitation } from '../../types/invitation.types';
import { expiresLabel } from '../../lib/invitation-ui';

interface ReceivedInvitationCardProps {
  invitation: ReceivedInvitation;
  onAccept: () => void;
  onReject: () => void;
  /** `accept` | `reject` mientras hay una acción en curso; `null` en reposo. */
  busy: 'accept' | 'reject' | null;
}

/**
 * Tarjeta de una invitación recibida con sus dos salidas: Aceptar y Rechazar (MVP-107, HU-2/HU-3).
 * Presentacional: la lógica vive en `NotificationsContext`. Se reutiliza en la campanita, el modal
 * no bloqueante y la pantalla de decisión del invitado sin Workspace.
 */
export const ReceivedInvitationCard: React.FC<ReceivedInvitationCardProps> = ({
  invitation,
  onAccept,
  onReject,
  busy,
}) => {
  const isBusy = busy !== null;

  return (
    <div className="rounded-xl border border-[#e5e2dd] bg-white p-4 space-y-3">
      <div className="space-y-1">
        <p className="font-bold text-[#1c1c19] leading-tight">{invitation.workspace.name}</p>
        <p className="text-sm text-[#45483c]">
          {invitation.invited_by
            ? `${invitation.invited_by} te invita a colaborar.`
            : 'Te invitan a colaborar en esta explotación.'}
        </p>
        <p className="text-xs text-[#76786b]">{expiresLabel(invitation.expires_at)}</p>
      </div>

      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={onAccept}
          disabled={isBusy}
          className="px-4 py-2 rounded-lg bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {busy === 'accept' ? 'Entrando…' : 'Aceptar'}
        </button>
        <button
          type="button"
          onClick={onReject}
          disabled={isBusy}
          className="px-4 py-2 rounded-lg border border-[#c6c8b8] text-[#1c1c19] text-sm font-semibold hover:bg-[#f0ede8] transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {busy === 'reject' ? 'Rechazando…' : 'Rechazar'}
        </button>
      </div>
    </div>
  );
};
