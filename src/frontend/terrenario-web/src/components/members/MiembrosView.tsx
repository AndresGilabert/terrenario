import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { createMemberService } from '../../services/member.service';
import { createWorkspaceLifecycleService } from '../../services/workspace-lifecycle.service';
import { HttpError } from '../../services/http-client';
import type {
  ResendInvitationResult,
  WorkspacePerson,
} from '../../types/member.types';
import type { WorkspaceMemberStatus } from '../../types/workspace.types';

const STATUS_BADGE: Record<WorkspaceMemberStatus, string> = {
  activo: 'bg-[#c9f16f] text-[#33450d]',
  invitado: 'bg-[#fdf6e3] text-[#8a6d1a]',
  revocado: 'bg-[#e5e2dd] text-[#76786b]',
};

const STATUS_LABEL: Record<WorkspaceMemberStatus, string> = {
  activo: 'Activo',
  invitado: 'Invitado',
  revocado: 'Sin acceso',
};

/**
 * Administración de personas y accesos del Workspace (MVP-204, HU-3/HU-4/HU-5). Lista unificada con
 * el estado de cada persona (activo/invitado/revocado), revocación de acceso (CA-7/CA-8) y reenvío de
 * invitaciones pendientes por email o por enlace (CA-6). Invitar reutiliza el flujo de MVP-103.
 *
 * MVP-207 (CA-4) completa la simetría de la pantalla: además de reenviar, ahora se puede **anular**
 * una invitación pendiente. Antes, invitar a un email equivocado no tenía marcha atrás: la única
 * acción de retirada (revocar) solo existía para quien ya había entrado.
 *
 * MVP-208 (CA-6/CA-7) la convierte en la **superficie única** de invitaciones pendientes: también
 * lista los enlaces compartibles, que antes solo aparecían en una lista de solo lectura y no se
 * podían anular desde ninguna pantalla —justo el caso de mayor riesgo si el enlace se filtra
 * (hallazgo R-15)—. Un enlace no es una persona, así que se presenta como lo que es: un acceso vivo
 * sin destinatario, con las mismas acciones de renovar y anular.
 */
export const MiembrosView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const memberService = useMemo(() => createMemberService(http), [http]);
  const lifecycle = useMemo(() => createWorkspaceLifecycleService(http), [http]);
  const { activeWorkspace, refreshContext } = useWorkspace();

  const [people, setPeople] = useState<WorkspacePerson[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [confirmingRevoke, setConfirmingRevoke] = useState<string | null>(null);
  const [confirmingCancel, setConfirmingCancel] = useState<string | null>(null);
  const [resendResults, setResendResults] = useState<Record<string, ResendInvitationResult>>({});
  const [copiedInvitationId, setCopiedInvitationId] = useState<string | null>(null);
  // MVP-807 — Salida voluntaria del Workspace.
  const [isConfirmingLeave, setConfirmingLeave] = useState(false);
  const [isLeaving, setLeaving] = useState(false);
  const [leaveError, setLeaveError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const response = await memberService.listPeople();
      setPeople(response.data);
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudieron cargar las personas del Workspace.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [memberService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const revoke = async (person: WorkspacePerson) => {
    if (!person.user_id) return;
    setBusyId(person.user_id);
    setActionError(null);
    try {
      await memberService.revokeMember(person.user_id);
      setConfirmingRevoke(null);
      await reload();
    } catch (error) {
      setActionError(
        error instanceof HttpError ? error.message : 'No se pudo retirar el acceso. Inténtalo de nuevo.'
      );
    } finally {
      setBusyId(null);
    }
  };

  /**
   * MVP-807 (HU-1, `P-048`) — Abandonar el Workspace.
   *
   * **Las guardas no se replican en el cliente.** Un propietario único y el último miembro activo no
   * pueden irse, pero quién lo es lo decide el servidor —con la misma guarda que usa la baja de
   * cuenta—, y su mensaje es el que se enseña. Adelantar aquí la condición sería una segunda copia de
   * la regla, que es justo lo que produjo `P-049` en esta misma pantalla.
   */
  const leave = async () => {
    setLeaving(true);
    setLeaveError(null);
    try {
      await lifecycle.leave();
      // El Workspace activo lo resuelve de nuevo el servidor: puede ser otro o ninguno.
      await refreshContext();
      navigate('/app', { replace: true });
    } catch (error) {
      setLeaveError(
        error instanceof HttpError ? error.message : 'No se pudo abandonar el Workspace. Inténtalo de nuevo.'
      );
      setLeaving(false);
    }
  };

  const resend = async (person: WorkspacePerson, deliverEmail: boolean) => {
    if (!person.invitation_id) return;
    setBusyId(person.invitation_id);
    setActionError(null);
    setCopiedInvitationId(null);
    try {
      const result = await memberService.resendInvitation(person.invitation_id, deliverEmail);
      setResendResults((prev) => ({ ...prev, [person.invitation_id!]: result }));
      await reload();
    } catch (error) {
      setActionError(
        error instanceof HttpError ? error.message : 'No se pudo reenviar la invitación. Inténtalo de nuevo.'
      );
    } finally {
      setBusyId(null);
    }
  };

  /**
   * Anula una invitación pendiente (CA-4). Tras anularla, el enlace deja de servir y la persona
   * desaparece de la lista, así que basta con recargar: no hay estado que conservar en pantalla.
   */
  const cancelInvitation = async (person: WorkspacePerson) => {
    if (!person.invitation_id) return;
    setBusyId(person.invitation_id);
    setActionError(null);
    try {
      await memberService.cancelInvitation(person.invitation_id);
      setConfirmingCancel(null);
      await reload();
    } catch (error) {
      setActionError(
        error instanceof HttpError ? error.message : 'No se pudo anular la invitación. Inténtalo de nuevo.'
      );
    } finally {
      setBusyId(null);
    }
  };

  const copyLink = async (invitationId: string, url: string) => {
    try {
      await navigator.clipboard.writeText(url);
      setCopiedInvitationId(invitationId);
    } catch {
      setActionError('No se pudo copiar el enlace. Selecciónalo y cópialo a mano.');
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Miembros y accesos</h2>
          <p className="text-xs text-[#76786b]">
            Quién forma parte del Workspace, en qué estado y qué invitaciones siguen en circulación
            —por email o por enlace—. Cualquier miembro puede invitar, renovar o anular una invitación,
            y retirar el acceso a otro.
          </p>
        </div>
        <button
          onClick={() => navigate('/app/invitations')}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
          <span>Invitar persona</span>
        </button>
      </div>

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}
      {actionError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {actionError}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <ul className="space-y-3">
          {people.map((person) => {
            const key = person.kind === 'member' ? `m-${person.user_id}` : `i-${person.invitation_id}`;
            const isBusy = busyId === (person.user_id ?? person.invitation_id);
            const resendResult = person.invitation_id ? resendResults[person.invitation_id] : undefined;
            const isLink = person.kind === 'invitation' && person.channel === 'enlace';
            const title = isLink ? 'Invitación por enlace' : (person.name ?? person.email ?? 'Sin nombre');
            const subtitle = isLink
              ? 'Enlace compartible, sin destinatario'
              : person.kind === 'member'
                ? person.email
                : null;

            return (
              <li
                key={key}
                className={`bg-white rounded-2xl border p-4 sm:p-5 ${
                  person.status === 'revocado' ? 'border-[#dcd9d2] bg-[#faf8f4]' : 'border-[#e5e2dd]'
                }`}
              >
                <div className="flex items-center justify-between gap-3 flex-wrap">
                  <div className="flex items-center gap-3 min-w-0">
                    <div
                      className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold shrink-0 ${
                        person.kind === 'invitation'
                          ? 'bg-[#f0ede8] text-[#8a6d1a]'
                          : 'bg-[#33450d] text-white'
                      }`}
                    >
                      {person.kind === 'invitation' ? (
                        <span className="material-symbols-outlined text-lg" aria-hidden="true">
                          {isLink ? 'link' : 'mail'}
                        </span>
                      ) : (
                        initials(person.name ?? person.email ?? '?')
                      )}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-[#1c1c19] truncate flex items-center gap-2">
                        {title}
                        {person.is_self && <span className="text-[10px] text-[#76786b] font-normal">(tú)</span>}
                        {person.role === 'workspace_owner' && (
                          <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-md bg-[#eef2e0] text-[#33450d]">
                            Propietario
                          </span>
                        )}
                      </p>
                      {/* El email solo se repite debajo cuando aporta algo: en un miembro es el dato
                          de contacto de su cuenta; en una invitación por email el título ya es el
                          email (se mostraba dos veces) y en un enlace no hay destinatario. */}
                      {subtitle && <p className="text-xs text-[#76786b] truncate">{subtitle}</p>}
                    </div>
                  </div>

                  <div className="flex items-center gap-2 shrink-0">
                    <span className={`text-[10px] font-bold px-2.5 py-1 rounded-full ${STATUS_BADGE[person.status]}`}>
                      {STATUS_LABEL[person.status].toUpperCase()}
                    </span>
                    {person.status === 'invitado' && person.is_expired && (
                      <span className="text-[10px] font-bold px-2 py-1 rounded-full bg-red-50 text-red-700">
                        CADUCADA
                      </span>
                    )}
                  </div>
                </div>

                {/* Acciones por estado */}
                {person.status === 'activo' && person.can_revoke && !person.is_self && (
                  <div className="mt-3 pt-3 border-t border-[#f0ede8] flex justify-end">
                    {confirmingRevoke === person.user_id ? (
                      <div className="flex items-center gap-2 text-xs">
                        <span className="text-[#45483c]">¿Retirar el acceso de esta persona?</span>
                        <button
                          onClick={() => void revoke(person)}
                          disabled={isBusy}
                          className="px-3 py-1.5 rounded-lg bg-[#ba1a1a] hover:bg-[#a01515] text-white font-semibold disabled:opacity-60"
                        >
                          {isBusy ? 'Retirando…' : 'Sí, retirar'}
                        </button>
                        <button
                          onClick={() => setConfirmingRevoke(null)}
                          disabled={isBusy}
                          className="px-3 py-1.5 rounded-lg text-[#45483c] hover:bg-[#f0ede8] font-semibold"
                        >
                          Cancelar
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => {
                          setConfirmingRevoke(person.user_id ?? null);
                          setActionError(null);
                        }}
                        className="text-xs font-semibold text-[#ba1a1a] hover:underline flex items-center gap-1"
                      >
                        <span className="material-symbols-outlined text-base" aria-hidden="true">person_remove</span>
                        Retirar acceso
                      </button>
                    )}
                  </div>
                )}

                {person.status === 'invitado' && (
                  <div className="mt-3 pt-3 border-t border-[#f0ede8] space-y-3">
                    <div className="flex items-center justify-between gap-3 flex-wrap">
                      {/* CA-7 — las mismas acciones en los dos canales: renovar el enlace y anular.
                          Reenviar el correo solo aparece donde hay a quién escribir. */}
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-xs text-[#76786b]">
                          {isLink ? 'Renovar enlace:' : 'Reenviar invitación:'}
                        </span>
                        {!isLink && (
                          <button
                            onClick={() => void resend(person, true)}
                            disabled={isBusy}
                            className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] flex items-center gap-1 disabled:opacity-60"
                          >
                            <span className="material-symbols-outlined text-sm" aria-hidden="true">mail</span>
                            Por email
                          </button>
                        )}
                        <button
                          onClick={() => void resend(person, false)}
                          disabled={isBusy}
                          className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] flex items-center gap-1 disabled:opacity-60"
                        >
                          <span className="material-symbols-outlined text-sm" aria-hidden="true">link</span>
                          {isLink ? 'Generar enlace nuevo' : 'Obtener enlace'}
                        </button>
                      </div>

                      {/* Anulación (MVP-207, CA-4): misma mecánica de confirmación en línea que
                          «Retirar acceso», para que retirar a un invitado y a un miembro se hagan
                          igual. */}
                      {confirmingCancel === person.invitation_id ? (
                        <div className="flex items-center gap-2 text-xs">
                          <span className="text-[#45483c]">
                            {isLink ? '¿Anular este enlace?' : '¿Anular esta invitación?'}
                          </span>
                          <button
                            onClick={() => void cancelInvitation(person)}
                            disabled={isBusy}
                            className="px-3 py-1.5 rounded-lg bg-[#ba1a1a] hover:bg-[#a01515] text-white font-semibold disabled:opacity-60"
                          >
                            {isBusy ? 'Anulando…' : 'Sí, anular'}
                          </button>
                          <button
                            onClick={() => setConfirmingCancel(null)}
                            disabled={isBusy}
                            className="px-3 py-1.5 rounded-lg text-[#45483c] hover:bg-[#f0ede8] font-semibold"
                          >
                            Cancelar
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => {
                            setConfirmingCancel(person.invitation_id ?? null);
                            setActionError(null);
                          }}
                          className="text-xs font-semibold text-[#ba1a1a] hover:underline flex items-center gap-1"
                        >
                          <span className="material-symbols-outlined text-base" aria-hidden="true">cancel</span>
                          {isLink ? 'Anular enlace' : 'Anular invitación'}
                        </button>
                      )}
                    </div>

                    {resendResult && (
                      <div className="bg-[#f6f3ee] border border-[#e5e2dd] rounded-xl p-3 space-y-2" role="status">
                        <p className="text-xs text-[#45483c]">
                          {resendResult.email_sent
                            ? `Invitación reenviada a ${resendResult.email}. También puedes compartir el enlace.`
                            : 'Nuevo enlace de un solo uso. Cópialo ahora: por seguridad no volveremos a mostrarlo. El enlace anterior deja de servir.'}
                        </p>
                        <div className="flex flex-col sm:flex-row gap-2">
                          <input
                            readOnly
                            value={resendResult.accept_url}
                            aria-label="Enlace de invitación"
                            onFocus={(e) => e.target.select()}
                            className="flex-1 px-3 py-2 bg-white border border-[#c6c8b8] rounded-lg text-xs font-medium text-[#1c1c19]"
                          />
                          <button
                            onClick={() => void copyLink(person.invitation_id!, resendResult.accept_url)}
                            className="px-3 py-2 rounded-lg bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold transition-colors"
                          >
                            {copiedInvitationId === person.invitation_id ? 'Copiado' : 'Copiar enlace'}
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                )}

                {person.status === 'revocado' && (
                  <p className="mt-3 pt-3 border-t border-[#f0ede8] text-xs text-[#76786b]">
                    Dejó de tener acceso. Para readmitirla, envíale una nueva invitación.
                  </p>
                )}
              </li>
            );
          })}
        </ul>
      )}

      {/* MVP-807 (HU-1, `P-048`) — La salida voluntaria. Va al final y en su propio bloque, separada
          de la lista: es una acción sobre uno mismo, no sobre otra persona, y mezclarla con las de la
          lista la haría parecer una más de las que se ejercen sobre los demás.

          Se ofrece siempre que haya Workspace activo. Quién puede irse lo decide el servidor: un
          propietario único y el último miembro activo reciben su negativa con el motivo. */}
      {activeWorkspace && (
        <section className="bg-white rounded-2xl border border-[#f0caca] p-5 space-y-3">
          <div>
            <h3 className="font-headline font-bold text-base text-[#ba1a1a]">Abandonar este Workspace</h3>
            <p className="text-xs text-[#76786b] mt-1">
              Dejarás de ver «{activeWorkspace.name}» en tu selector y de aparecer como responsable
              seleccionable. <strong>Lo que registraste no se borra</strong>: las labores, cosechas y
              compras que apuntaste siguen ahí con tu nombre. Para volver a entrar necesitarás una
              invitación nueva.
            </p>
          </div>

          {leaveError && (
            <p role="alert" className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-xl p-3">
              {leaveError}
            </p>
          )}

          {isConfirmingLeave ? (
            <div className="flex items-center gap-2 flex-wrap text-xs">
              <span className="text-[#45483c]">¿Seguro que quieres salir de «{activeWorkspace.name}»?</span>
              <button
                type="button"
                onClick={() => void leave()}
                disabled={isLeaving}
                className="px-3 py-1.5 rounded-lg bg-[#ba1a1a] hover:bg-[#a01515] text-white font-semibold disabled:opacity-60"
              >
                {isLeaving ? 'Saliendo…' : 'Sí, abandonar'}
              </button>
              <button
                type="button"
                onClick={() => setConfirmingLeave(false)}
                disabled={isLeaving}
                className="px-3 py-1.5 rounded-lg text-[#45483c] hover:bg-[#f0ede8] font-semibold"
              >
                Cancelar
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => {
                setConfirmingLeave(true);
                setLeaveError(null);
              }}
              className="text-xs font-semibold text-[#ba1a1a] hover:underline flex items-center gap-1"
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">logout</span>
              Abandonar este Workspace
            </button>
          )}
        </section>
      )}
    </div>
  );
};

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((p) => p.charAt(0).toUpperCase()).join('') || '?';
}
