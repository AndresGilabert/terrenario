import React from 'react';
import { useNotifications } from '../../contexts/NotificationsContext';
import { ReceivedInvitationCard } from '../notifications/ReceivedInvitationCard';
import { useInvitationActions } from '../notifications/useInvitationActions';

interface ReceivedInvitationsPageProps {
  /** Salida secundaria: crear el propio Workspace en lugar de aceptar una invitación. */
  onCreateOwn: () => void;
}

/**
 * MVP-107 — Pantalla de bienvenida para quien inicia sesión con invitaciones pendientes y ningún
 * Workspace propio (decisión de producto: priorizar la invitación). Da protagonismo a aceptar o
 * rechazar, con un enlace secundario para crear el Workspace propio, de modo que nunca se sienta
 * obligado a crear uno.
 */
export const ReceivedInvitationsPage: React.FC<ReceivedInvitationsPageProps> = ({ onCreateOwn }) => {
  const { receivedInvitations } = useNotifications();
  const { busyFor, error, acceptInvitation, rejectInvitation } = useInvitationActions();

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span className="material-symbols-outlined text-base" aria-hidden="true">mail</span>
            <span>Te están esperando</span>
          </div>
          <h1 className="font-bold text-2xl text-[#1c1c19]">
            Tienes {receivedInvitations.length === 1 ? 'una invitación' : 'invitaciones'} pendiente
            {receivedInvitations.length === 1 ? '' : 's'}
          </h1>
          <p className="text-sm text-[#45483c]">
            Acepta para unirte y empezar a colaborar, o crea tu propio Workspace si prefieres empezar
            de cero.
          </p>
        </div>

        {error && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {error}
          </p>
        )}

        <div className="space-y-3">
          {receivedInvitations.map((invitation) => (
            <ReceivedInvitationCard
              key={invitation.id}
              invitation={invitation}
              busy={busyFor(invitation.id)}
              onAccept={() => void acceptInvitation(invitation.id)}
              onReject={() => void rejectInvitation(invitation.id)}
            />
          ))}
        </div>

        <div className="pt-2 border-t border-[#e5e2dd] text-center">
          <button
            type="button"
            onClick={onCreateOwn}
            className="text-sm font-semibold text-[#33450d] hover:text-[#4a5d23] underline underline-offset-2"
          >
            Prefiero crear mi propio Workspace
          </button>
        </div>
      </div>
    </div>
  );
};
