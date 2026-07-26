import React, { useEffect } from 'react';
import { useNotifications } from '../../contexts/NotificationsContext';
import { ReceivedInvitationCard } from './ReceivedInvitationCard';
import { useInvitationActions } from './useInvitationActions';

/**
 * MVP-107 (HU-2, CA-3) — Modal no bloqueante que aparece al llegar a la operativa con una invitación
 * nueva pendiente. Se puede cerrar dejándola pendiente: no es una puerta obligatoria. Aceptar sitúa
 * la sesión en el Workspace; rechazar la declina sin sacar de la plataforma.
 */
export const InvitationModal: React.FC = () => {
  const { newInvitation, dismissNew } = useNotifications();
  const { busyFor, error, acceptInvitation, rejectInvitation } = useInvitationActions();

  // Cerrar con Escape, como cualquier modal descartable.
  useEffect(() => {
    if (!newInvitation) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') dismissNew();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [newInvitation, dismissNew]);

  if (!newInvitation) return null;

  const busy = busyFor(newInvitation.id);

  return (
    <div
      className="fixed inset-0 z-40 flex items-center justify-center p-4 bg-black/40"
      role="presentation"
      onMouseDown={(event) => {
        // Clic en el velo = cerrar y dejar pendiente. No cierra si hay una acción en curso.
        if (event.target === event.currentTarget && busy === null) dismissNew();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="invitation-modal-title"
        className="w-full max-w-md bg-[#fcf9f4] rounded-2xl border border-[#e5e2dd] shadow-xl p-6 space-y-4"
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span className="material-symbols-outlined text-base" aria-hidden="true">mail</span>
            <span id="invitation-modal-title">Tienes una invitación</span>
          </div>
          <button
            type="button"
            onClick={dismissNew}
            disabled={busy !== null}
            aria-label="Cerrar y decidir más tarde"
            className="text-[#76786b] hover:text-[#1c1c19] text-xl leading-none px-1 disabled:opacity-60"
          >
            ×
          </button>
        </div>

        {error && (
          <p role="alert" className="text-sm text-red-700">
            {error}
          </p>
        )}

        <ReceivedInvitationCard
          invitation={newInvitation}
          busy={busy}
          onAccept={() => void acceptInvitation(newInvitation.id)}
          onReject={() => void rejectInvitation(newInvitation.id)}
        />

        <button
          type="button"
          onClick={dismissNew}
          disabled={busy !== null}
          className="w-full text-center text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] py-1 disabled:opacity-60"
        >
          Decidir más tarde
        </button>
      </div>
    </div>
  );
};
