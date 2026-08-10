import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useNotifications } from '../../contexts/NotificationsContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { createReactivationService } from '../../services/workspace-lifecycle.service';
import { HttpError } from '../../services/http-client';
import type { ClosedWorkspace, ReactivationRequest } from '../../types/workspace-lifecycle.types';

/**
 * MVP-206 (HU-6) — Lo que le queda pendiente a quien dio de baja Workspaces: autorizar o denegar las
 * solicitudes de traspaso de sus miembros (CA-7/CA-10) y volver a levantar por su cuenta los que dio
 * de baja (cara reversible de CA-2, y única vía cuando no había a quién notificar).
 *
 * Fuera del shell y de la guarda de Workspace: quien dio de baja su único Workspace no tiene
 * contexto activo, y esta pantalla es justo su forma de recuperarlo.
 */
export const ReactivationInboxPage: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const reactivations = useMemo(() => createReactivationService(http), [http]);
  const { refreshContext } = useWorkspace();
  // MVP-808 (CA-4) — Resuelta la solicitud, el aviso de la campanita tiene que irse con ella. La
  // bandeja no se entera sola: sin esto seguiría anunciando una decisión que ya está tomada hasta el
  // siguiente refresco por foco.
  const { refresh: refreshNotifications } = useNotifications();

  const [requests, setRequests] = useState<ReactivationRequest[]>([]);
  const [closed, setClosed] = useState<ClosedWorkspace[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const [pending, mine] = await Promise.all([
        reactivations.listPendingAuthorizations(),
        reactivations.listClosed(),
      ]);
      setRequests(pending.data);
      setClosed(mine.data);
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError ? error.message : 'No se pudieron cargar tus Workspaces dados de baja.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [reactivations]);

  useEffect(() => {
    void load();
  }, [load]);

  const run = async (id: string, action: () => Promise<unknown>) => {
    setBusyId(id);
    setErrorMessage(null);
    try {
      await action();
      await refreshContext();
      await Promise.all([load(), refreshNotifications()]);
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError ? error.message : 'No se pudo completar la operación. Inténtalo de nuevo.'
      );
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] p-4 sm:p-6 md:p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-headline font-extrabold text-xl text-[#1c1c19]">
              Workspaces dados de baja
            </h1>
            <p className="text-xs text-[#76786b]">
              Solicitudes de traspaso que tienes que decidir y Workspaces que puedes volver a
              levantar.
            </p>
          </div>
          <button
            type="button"
            onClick={() => navigate('/app')}
            className="text-xs font-semibold text-[#33450d] hover:underline"
          >
            Ir a Terrenario
          </button>
        </div>

        {errorMessage && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-16">
            <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            <section className="space-y-3">
              <h2 className="font-headline font-bold text-base text-[#1c1c19]">
                Solicitudes pendientes de tu decisión
              </h2>

              {requests.length === 0 && (
                <p className="text-sm text-[#76786b] bg-white rounded-2xl border border-[#e5e2dd] p-4">
                  No hay ninguna solicitud esperándote.
                </p>
              )}

              {requests.map((request) => (
                <div
                  key={request.id}
                  className="bg-white rounded-2xl border border-[#e5e2dd] p-4 sm:p-5 space-y-3"
                >
                  <div>
                    <p className="text-sm font-semibold text-[#1c1c19]">{request.workspace.name}</p>
                    <p className="text-xs text-[#76786b]">
                      {request.requested_by.name} ({request.requested_by.email}) pide que se lo
                      traspases y se reactive.
                    </p>
                  </div>
                  <p className="text-xs text-[#45483c]">
                    Si autorizas, el Workspace vuelve con esa persona como propietaria y tú te quedas
                    dentro como miembro.
                  </p>
                  <div className="flex flex-wrap gap-2 justify-end">
                    <button
                      type="button"
                      disabled={busyId === request.id}
                      onClick={() => void run(request.id, () => reactivations.deny(request.id))}
                      className="px-4 py-2 rounded-xl border border-[#c6c8b8] text-[#1c1c19] text-xs font-semibold hover:bg-[#f0ede8] disabled:opacity-60"
                    >
                      Rechazar
                    </button>
                    <button
                      type="button"
                      disabled={busyId === request.id}
                      onClick={() => void run(request.id, () => reactivations.authorize(request.id))}
                      className="px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold disabled:opacity-60"
                    >
                      {busyId === request.id ? 'Aplicando…' : 'Autorizar traspaso'}
                    </button>
                  </div>
                </div>
              ))}
            </section>

            <section className="space-y-3">
              <h2 className="font-headline font-bold text-base text-[#1c1c19]">
                Workspaces que diste de baja
              </h2>

              {closed.length === 0 && (
                <p className="text-sm text-[#76786b] bg-white rounded-2xl border border-[#e5e2dd] p-4">
                  No tienes ningún Workspace dado de baja.
                </p>
              )}

              {closed.map((workspace) => (
                <div
                  key={workspace.id}
                  className="bg-white rounded-2xl border border-[#e5e2dd] p-4 sm:p-5 flex flex-wrap items-center justify-between gap-3"
                >
                  <div>
                    <p className="text-sm font-semibold text-[#1c1c19]">{workspace.name}</p>
                    <p className="text-xs text-[#76786b]">
                      Dado de baja el {new Date(workspace.closed_at).toLocaleDateString('es-ES')}. Los
                      datos siguen guardados.
                    </p>
                  </div>
                  <button
                    type="button"
                    disabled={busyId === workspace.id}
                    onClick={() => void run(workspace.id, () => reactivations.reopen(workspace.id))}
                    className="px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold disabled:opacity-60"
                  >
                    {busyId === workspace.id ? 'Levantando…' : 'Volver a activar'}
                  </button>
                </div>
              ))}
            </section>
          </>
        )}
      </div>
    </div>
  );
};
