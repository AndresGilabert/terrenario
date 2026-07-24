import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { invitationService, InvitationServiceError } from '../../services/invitation.service';
import type {
  CreatedInvitation,
  InvitationChannel,
  PendingInvitation,
} from '../../types/invitation.types';

const EMAIL_MAX_LENGTH = 320;

const CHANNEL_LABELS: Record<InvitationChannel, string> = {
  email: 'Por email',
  enlace: 'Por enlace',
};

/**
 * MVP-103 — Invitar a otra persona al Workspace activo (HU-1).
 * Referencia visual: `prototype/terrenario-mvp/src/components/AjustesView.tsx`.
 */
export const InvitePeoplePage: React.FC = () => {
  const navigate = useNavigate();
  const { getAccessToken } = useAuth();
  const { activeWorkspace } = useWorkspace();

  const [channel, setChannel] = useState<InvitationChannel>('email');
  const [email, setEmail] = useState('');
  const [createdInvitation, setCreatedInvitation] = useState<CreatedInvitation | null>(null);
  const [pendingInvitations, setPendingInvitations] = useState<PendingInvitation[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLinkCopied, setIsLinkCopied] = useState(false);

  const loadPendingInvitations = useCallback(async () => {
    const accessToken = await getAccessToken();
    if (!accessToken) return;

    try {
      setPendingInvitations(await invitationService.listPendingInvitations(accessToken));
    } catch {
      // La lista es informativa: si falla, la pantalla sigue permitiendo invitar.
      setPendingInvitations([]);
    }
  }, [getAccessToken]);

  useEffect(() => {
    void loadPendingInvitations();
  }, [loadPendingInvitations]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    const normalizedEmail = email.trim();
    if (channel === 'email' && !normalizedEmail) {
      setErrorMessage('Escribe el email de la persona a la que quieres invitar.');
      return;
    }

    setErrorMessage(null);
    setCreatedInvitation(null);
    setIsLinkCopied(false);
    setIsSubmitting(true);

    try {
      const accessToken = await getAccessToken();
      if (!accessToken) throw new Error('Sesión no válida.');

      const invitation = await invitationService.createInvitation(
        channel,
        channel === 'email' ? normalizedEmail : null,
        accessToken
      );

      setCreatedInvitation(invitation);
      setEmail('');
      await loadPendingInvitations();
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof InvitationServiceError
          ? error.message
          : 'No se pudo crear la invitación. Inténtalo de nuevo.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCopyLink = async () => {
    if (!createdInvitation) return;

    try {
      await navigator.clipboard.writeText(createdInvitation.accept_url);
      setIsLinkCopied(true);
    } catch {
      setErrorMessage('No se pudo copiar el enlace. Selecciónalo y cópialo a mano.');
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] p-4 sm:p-8">
      <div className="mx-auto w-full max-w-2xl space-y-6">
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-1">
          <h1 className="font-bold text-xl text-[#1c1c19]">Invitar a tu Workspace</h1>
          <p className="text-xs text-[#76786b]">
            {activeWorkspace
              ? `Comparte ${activeWorkspace.name} con quien trabaje contigo. Todos los miembros pueden operar y administrar el Workspace.`
              : 'Comparte tu explotación con quien trabaje contigo.'}
          </p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-5"
          noValidate
        >
          <fieldset className="space-y-2">
            <legend className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Cómo quieres invitar
            </legend>
            <div className="flex gap-2">
              {(Object.keys(CHANNEL_LABELS) as InvitationChannel[]).map((option) => (
                <button
                  key={option}
                  type="button"
                  onClick={() => setChannel(option)}
                  aria-pressed={channel === option}
                  disabled={isSubmitting}
                  className={`px-4 py-2 rounded-xl text-xs font-bold border transition-colors disabled:opacity-60 ${
                    channel === option
                      ? 'bg-[#33450d] text-white border-[#33450d]'
                      : 'bg-[#f6f3ee] text-[#45483c] border-[#c6c8b8] hover:bg-[#f0ede8]'
                  }`}
                >
                  {CHANNEL_LABELS[option]}
                </button>
              ))}
            </div>
          </fieldset>

          {channel === 'email' ? (
            <div className="space-y-2">
              <label
                htmlFor="invitation-email"
                className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
              >
                Email de la persona invitada
              </label>
              <input
                id="invitation-email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="ej. encargado@ejemplo.com"
                maxLength={EMAIL_MAX_LENGTH}
                autoComplete="off"
                disabled={isSubmitting}
                aria-invalid={errorMessage !== null}
                className="w-full px-4 py-3 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white transition-all font-medium disabled:opacity-60"
              />
              <p className="text-xs text-[#76786b]">
                Solo podrá aceptarla quien acceda con esa misma cuenta de Google.
              </p>
            </div>
          ) : (
            <p className="text-xs text-[#76786b] bg-[#f0ede8] rounded-xl p-4 border border-[#e5e2dd]">
              Generaremos un enlace de un solo uso. Compártelo únicamente con la persona que quieras
              incorporar: quien lo abra e inicie sesión entrará en el Workspace.
            </p>
          )}

          {errorMessage && (
            <div
              role="alert"
              className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
            >
              {errorMessage}
            </div>
          )}

          <div className="flex items-center justify-between pt-2">
            <button
              type="button"
              onClick={() => navigate('/app')}
              className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19]"
            >
              Volver
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Creando…' : 'Crear invitación'}
            </button>
          </div>
        </form>

        {createdInvitation && (
          <div
            className="bg-white p-6 rounded-2xl border border-[#c9f16f] shadow-xs space-y-4"
            role="status"
          >
            <div className="space-y-1">
              <h2 className="font-bold text-base text-[#1c1c19]">Invitación lista</h2>
              <p className="text-xs text-[#45483c]">
                {createdInvitation.channel === 'email' && createdInvitation.email_sent
                  ? `Hemos enviado la invitación a ${createdInvitation.email}. También puedes compartir el enlace.`
                  : 'Copia el enlace ahora: por seguridad no volveremos a mostrarlo.'}
              </p>
              {createdInvitation.channel === 'email' && !createdInvitation.email_sent && (
                <p className="text-xs font-semibold text-[#8a5a00]">
                  No pudimos enviar el email. Comparte el enlace por otro medio.
                </p>
              )}
            </div>

            <div className="flex flex-col sm:flex-row gap-2">
              <input
                readOnly
                value={createdInvitation.accept_url}
                aria-label="Enlace de invitación"
                onFocus={(event) => event.target.select()}
                className="flex-1 px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
              />
              <button
                type="button"
                onClick={() => void handleCopyLink()}
                className="px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold transition-colors"
              >
                {isLinkCopied ? 'Copiado' : 'Copiar enlace'}
              </button>
            </div>

            <p className="text-xs text-[#76786b]">
              Caduca el {formatDate(createdInvitation.expires_at)}.
            </p>
          </div>
        )}

        <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-3">
          <h2 className="font-bold text-base text-[#1c1c19] border-b border-[#f0ede8] pb-2">
            Invitaciones pendientes
          </h2>

          {pendingInvitations.length === 0 ? (
            <p className="text-xs text-[#76786b]">No hay invitaciones pendientes ahora mismo.</p>
          ) : (
            <ul className="divide-y divide-[#f0ede8]">
              {pendingInvitations.map((invitation) => (
                <li key={invitation.id} className="py-3 flex items-center justify-between gap-4">
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-[#1c1c19] truncate">
                      {invitation.email ?? 'Enlace compartible'}
                    </p>
                    <p className="text-xs text-[#76786b]">
                      {CHANNEL_LABELS[invitation.channel]} · caduca el{' '}
                      {formatDate(invitation.expires_at)}
                    </p>
                  </div>
                  <span className="shrink-0 px-3 py-1 rounded-full bg-[#f0ede8] text-[#45483c] text-xs font-bold">
                    {invitation.status}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
};

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
