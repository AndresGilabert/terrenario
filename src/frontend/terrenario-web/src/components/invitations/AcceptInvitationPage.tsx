import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { invitationService, InvitationServiceError } from '../../services/invitation.service';
import type { InvitationPreview } from '../../types/invitation.types';
import { shouldOfferGoogleSignup, viewerReasonMessage } from '../../lib/invitation-ui';
import {
  GOOGLE_ACCOUNT_SIGNUP_LABEL,
  GOOGLE_ACCOUNT_SIGNUP_URL,
} from '../../lib/google-account';

/**
 * MVP-103 / MVP-107 — Pantalla de decisión al abrir un enlace de invitación (HU-2). Informa la
 * aptitud de la cuenta antes de aceptar (R-C), permite aceptar o rechazar, y nunca deja al usuario
 * sin salida a la plataforma. Ya no ofrece "Usar otra cuenta" (que cerraba sesión sin sentido).
 */
export const AcceptInvitationPage: React.FC = () => {
  const { token = '' } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const { getAccessToken } = useAuth();
  const { acceptInvitation } = useWorkspace();

  const [invitation, setInvitation] = useState<InvitationPreview | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [busy, setBusy] = useState<'accept' | 'reject' | null>(null);

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
            error instanceof InvitationServiceError ? error.message : 'No se pudo cargar la invitación.'
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

  const goToPlatform = () => navigate('/app', { replace: true });

  const handleAccept = async () => {
    setErrorMessage(null);
    setBusy('accept');
    try {
      await acceptInvitation(token);
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof InvitationServiceError
          ? error.message
          : 'No se pudo aceptar la invitación. Inténtalo de nuevo.'
      );
      setBusy(null);
    }
  };

  const handleReject = async () => {
    setErrorMessage(null);
    setBusy('reject');
    try {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');
      await invitationService.rejectInvitation(token, accessToken);
      // Rechazar no cierra sesión: se continúa hacia la plataforma (CA-2).
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof InvitationServiceError
          ? error.message
          : 'No se pudo rechazar la invitación. Inténtalo de nuevo.'
      );
      setBusy(null);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const canAccept = invitation?.viewer.can_accept ?? false;
  const aptitudeMessage = invitation ? viewerReasonMessage(invitation.viewer.reason) : null;
  const alreadyMember = invitation?.viewer.reason === 'already_member';
  const offerGoogleSignup = shouldOfferGoogleSignup(invitation?.viewer.reason ?? null);

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
          <span className="material-symbols-outlined text-base" aria-hidden="true">eco</span>
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

        {/* Aptitud anticipada (R-C): informa antes de pulsar, sin revelar el email destinatario. */}
        {aptitudeMessage && (
          <p
            role={canAccept ? undefined : 'alert'}
            className={
              canAccept
                ? 'p-3 rounded-xl bg-[#f0f4e3] border border-[#d5e0b5] text-[#33450d] text-sm'
                : 'p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm'
            }
          >
            {aptitudeMessage}
            {/* MVP-712 (CA-3) — El aviso explicaba el problema y no la salida: quien fue invitado en
                una dirección sin Cuenta de Google se quedaba sin nada que hacer. El enlace va dentro
                del propio aviso porque es la continuación de su última frase, y en pestaña nueva
                para no perder la invitación al ir a darse de alta. */}
            {offerGoogleSignup && (
              <>
                {' '}
                <a
                  href={GOOGLE_ACCOUNT_SIGNUP_URL}
                  target="_blank"
                  rel="noreferrer"
                  className="font-semibold underline"
                >
                  {GOOGLE_ACCOUNT_SIGNUP_LABEL}
                </a>
              </>
            )}
          </p>
        )}

        {errorMessage && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </p>
        )}

        <div className="flex flex-wrap items-center justify-end gap-3 pt-2">
          {/* Salida siempre disponible hacia la plataforma (mis Workspaces u onboarding). */}
          <button
            type="button"
            onClick={goToPlatform}
            disabled={busy !== null}
            className="px-4 py-2 text-sm font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-60"
          >
            {canAccept && !alreadyMember ? 'Ahora no' : 'Ir a Terrenario'}
          </button>

          {canAccept && !alreadyMember && (
            <button
              type="button"
              onClick={() => void handleReject()}
              disabled={busy !== null}
              className="px-5 py-3 rounded-xl border border-[#c6c8b8] text-[#1c1c19] font-semibold text-sm hover:bg-[#f0ede8] transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {busy === 'reject' ? 'Rechazando…' : 'Rechazar'}
            </button>
          )}

          {canAccept && (
            <button
              type="button"
              onClick={() => void handleAccept()}
              disabled={busy !== null}
              className="px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {busy === 'accept'
                ? 'Entrando…'
                : alreadyMember
                  ? 'Entrar al Workspace'
                  : 'Unirme al Workspace'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
