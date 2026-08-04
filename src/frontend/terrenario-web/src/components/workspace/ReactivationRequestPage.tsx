import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { createReactivationService } from '../../services/workspace-lifecycle.service';
import { HttpError } from '../../services/http-client';
import type { ReactivationPreview } from '../../types/workspace-lifecycle.types';

/**
 * MVP-206 (HU-5) — Pantalla del enlace que recibe un miembro cuando se da de baja un Workspace del
 * que formaba parte. Informa antes de pulsar (aptitud del enlace) y permite **solicitar** el
 * traspaso y la reactivación; la decisión final es de quien dio de baja (CA-7).
 *
 * No exige Workspace activo: el Workspace de este enlace está dado de baja y puede ser el único que
 * tuviera la persona. Siempre hay salida hacia la plataforma.
 */
export const ReactivationRequestPage: React.FC = () => {
  const { token = '' } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const http = useApiClient();
  const reactivations = useMemo(() => createReactivationService(http), [http]);

  const [preview, setPreview] = useState<ReactivationPreview | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isBusy, setIsBusy] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [requested, setRequested] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      setPreview(await reactivations.preview(token));
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError ? error.message : 'No se pudo cargar el enlace de reactivación.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [reactivations, token]);

  useEffect(() => {
    void load();
  }, [load]);

  const submit = async () => {
    setErrorMessage(null);
    setIsBusy(true);
    try {
      setPreview(await reactivations.request(token));
      setRequested(true);
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError ? error.message : 'No se pudo enviar la solicitud. Inténtalo de nuevo.'
      );
    } finally {
      setIsBusy(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
          <span className="material-symbols-outlined text-base" aria-hidden="true">eco</span>
          <span>Workspace dado de baja</span>
        </div>

        {preview && (
          <div className="space-y-1.5">
            <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">{preview.workspace.name}</h1>
            <p className="text-sm text-[#45483c]">
              {preview.closed_by
                ? `${preview.closed_by} dio de baja este Workspace.`
                : 'Este Workspace se ha dado de baja.'}{' '}
              No se ha borrado nada: puedes pedir que te lo traspasen y se reactive.
            </p>
          </div>
        )}

        {requested && (
          <p role="status" className="p-3 rounded-xl bg-[#f0f4e3] border border-[#d5e0b5] text-[#33450d] text-sm">
            Solicitud enviada. Quien dio de baja el Workspace recibirá el aviso y decidirá si te lo
            traspasa. Te aparecerá de nuevo en tu selector si lo autoriza.
          </p>
        )}

        {!requested && preview && !preview.can_request && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {preview.is_expired
              ? 'Este enlace ha caducado. Pídeselo de nuevo a quien dio de baja el Workspace.'
              : preview.status === 'solicitada'
                ? 'Ya has usado este enlace: tu solicitud está pendiente de autorización.'
                : 'Este enlace ya no está disponible.'}
          </p>
        )}

        {errorMessage && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </p>
        )}

        <div className="flex flex-wrap items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={() => navigate('/app', { replace: true })}
            disabled={isBusy}
            className="px-4 py-2 text-sm font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-60"
          >
            Ir a Terrenario
          </button>

          {!requested && preview?.can_request && (
            <button
              type="button"
              onClick={() => void submit()}
              disabled={isBusy}
              className="px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60"
            >
              {isBusy ? 'Enviando…' : 'Solicitar traspaso y reactivación'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
