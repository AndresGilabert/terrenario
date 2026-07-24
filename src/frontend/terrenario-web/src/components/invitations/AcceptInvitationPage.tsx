import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { invitationService, InvitationServiceError } from '../../services/invitation.service';
import type { InvitationPreview } from '../../types/invitation.types';

/**
 * MVP-103 — Aceptación de una invitación por parte de la persona invitada (HU-2).
 * La ruta va protegida: el enlace no abre ninguna vía de acceso sin sesión iniciada.
 */
export const AcceptInvitationPage: React.FC = () => {
  const { token = '' } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const { getAccessToken, logout } = useAuth();
  const { acceptInvitation } = useWorkspace();

  const [invitation, setInvitation] = useState<InvitationPreview | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isAccepting, setIsAccepting] = useState(false);

  // El token de sesión rota (refresh, aceptación); la referencia evita recargar la invitación
  // cada vez que cambia.
  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  useEffect(() => {
    let cancelled = false;

    (async () => {
      const accessToken = await getAccessTokenRef.current();
      if (cancelled) return;

      if (!accessToken) {
        setErrorMessage('Tu sesión ha expirado. Vuelve a entrar y abre de nuevo el enlace.');
        setIsLoading(false);
        return;
      }

      try {
        const preview = await invitationService.getInvitation(token, accessToken);
        if (!cancelled) setInvitation(preview);
      } catch (error: unknown) {
        if (!cancelled) {
          setErrorMessage(
            error instanceof InvitationServiceError
              ? error.message
              : 'No se pudo cargar la invitación.'
          );
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [token]);

  const handleAccept = async () => {
    setErrorMessage(null);
    setIsAccepting(true);

    try {
      await acceptInvitation(token);
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof InvitationServiceError
          ? error.message
          : 'No se pudo aceptar la invitación. Inténtalo de nuevo.'
      );
      setIsAccepting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const isUsable =
    invitation !== null && invitation.status === 'pendiente' && !invitation.is_expired;

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
          <span aria-hidden="true">🌿</span>
          <span>Invitación a un Workspace</span>
        </div>

        {invitation && (
          <div className="space-y-1.5">
            <h1 className="font-bold text-2xl text-[#1c1c19]">{invitation.workspace.name}</h1>
            <p className="text-sm text-[#45483c]">
              {invitation.invited_by
                ? `${invitation.invited_by} te invita a colaborar en esta explotación.`
                : 'Te invitan a colaborar en esta explotación.'}
            </p>
          </div>
        )}

        {invitation && !isUsable && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {invitation.is_expired
              ? 'Esta invitación ha caducado. Pide una nueva a quien te invitó.'
              : 'Esta invitación ya se ha utilizado.'}
          </p>
        )}

        {errorMessage && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </p>
        )}

        <div className="flex items-center justify-between pt-2">
          <button
            type="button"
            onClick={() => void logout()}
            className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19]"
          >
            Usar otra cuenta
          </button>
          {isUsable ? (
            <button
              type="button"
              onClick={() => void handleAccept()}
              disabled={isAccepting}
              className="px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isAccepting ? 'Entrando…' : 'Unirme al Workspace'}
            </button>
          ) : (
            <button
              type="button"
              onClick={() => navigate('/app', { replace: true })}
              className="px-6 py-3 rounded-xl border border-[#c6c8b8] text-[#1c1c19] font-semibold text-sm hover:bg-[#f0ede8] transition-colors"
            >
              Ir a Terrenario
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
