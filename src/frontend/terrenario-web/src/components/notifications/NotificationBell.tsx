import React, { useEffect, useRef, useState } from 'react';
import { useNotifications } from '../../contexts/NotificationsContext';
import { PendingReactivationCard } from './PendingReactivationCard';
import { ReceivedInvitationCard } from './ReceivedInvitationCard';
import { useInvitationActions } from './useInvitationActions';

/**
 * MVP-107 (HU-3, CA-3) — Campanita en la cabecera con el número de avisos pendientes y una bandeja
 * para atenderlos sin salir de la operativa.
 *
 * MVP-808 (CA-3) — La bandeja deja de ser solo de invitaciones: también lista las solicitudes de
 * reactivación de Workspace que esperan la decisión de esta cuenta, que hasta ahora únicamente se
 * avisaban por correo.
 */
export const NotificationBell: React.FC = () => {
  const { receivedInvitations, pendingReactivations, pendingCount } = useNotifications();
  const { busyFor, error, acceptInvitation, rejectInvitation } = useInvitationActions();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Cierra la bandeja al pulsar fuera o con Escape: comportamiento esperado de un menú emergente.
  useEffect(() => {
    if (!isOpen) return;

    const onPointerDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsOpen(false);
    };

    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [isOpen]);

  const hasPending = pendingCount > 0;
  const hasInvitations = receivedInvitations.length > 0;
  const hasReactivations = pendingReactivations.length > 0;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        className="relative p-2 rounded-lg text-[#45483c] hover:bg-[#f0ede8] transition-colors"
        aria-label={
          hasPending ? `Notificaciones: ${pendingCount} aviso(s) pendiente(s)` : 'Notificaciones'
        }
        aria-haspopup="true"
        aria-expanded={isOpen}
      >
        <span className="material-symbols-outlined" aria-hidden="true">notifications</span>
        {hasPending && (
          <span
            className="absolute -top-0.5 -right-0.5 min-w-4 h-4 px-1 rounded-full bg-[#b3261e] text-white text-[10px] font-bold flex items-center justify-center"
            aria-hidden="true"
          >
            {pendingCount > 9 ? '9+' : pendingCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          role="dialog"
          aria-label="Avisos pendientes"
          className="absolute right-0 mt-2 w-[min(22rem,calc(100vw-2rem))] max-h-[70vh] overflow-y-auto rounded-2xl border border-[#e5e2dd] bg-[#fcf9f4] shadow-xl p-3 z-30 space-y-3"
        >
          <div className="flex items-center justify-between px-1">
            <h3 className="text-sm font-bold text-[#1c1c19]">Avisos</h3>
            {hasPending && (
              <span className="text-xs font-semibold text-[#33450d]">{pendingCount} pendiente(s)</span>
            )}
          </div>

          {error && (
            <p role="alert" className="px-1 text-sm text-red-700">
              {error}
            </p>
          )}

          {!hasPending && (
            <p className="px-1 py-6 text-center text-sm text-[#76786b]">
              No tienes avisos pendientes.
            </p>
          )}

          {/* Los dos tipos van con su encabezado solo cuando conviven: con uno solo, un título de
              sección sobre una única tarjeta es ruido. */}
          {hasInvitations && (
            <div className="space-y-2">
              {hasReactivations && (
                <h4 className="px-1 text-xs font-semibold uppercase tracking-wide text-[#76786b]">
                  Invitaciones
                </h4>
              )}
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
          )}

          {hasReactivations && (
            <div className="space-y-2">
              {hasInvitations && (
                <h4 className="px-1 text-xs font-semibold uppercase tracking-wide text-[#76786b]">
                  Reactivaciones
                </h4>
              )}
              {pendingReactivations.map((request) => (
                <PendingReactivationCard
                  key={request.id}
                  request={request}
                  onNavigate={() => setIsOpen(false)}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
