import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { createWorkspaceLifecycleService } from '../../services/workspace-lifecycle.service';
import { HttpError } from '../../services/http-client';
import type { WorkspaceClosureOptions } from '../../types/workspace-lifecycle.types';
import { CloseWorkspaceModal } from './CloseWorkspaceModal';

/**
 * MVP-206 — Ajustes del Workspace activo: renombrar (HU-1/CA-1) y la zona de propiedad y baja
 * (HU-2/HU-3/HU-4). Referencia visual: `prototype/terrenario-mvp/src/components/AjustesView.tsx`;
 * el «perfil del titular» del prototipo no se porta porque los datos de la cuenta vienen de Google
 * y no son editables en el MVP (RN-036).
 */
export const AjustesView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const lifecycle = useMemo(() => createWorkspaceLifecycleService(http), [http]);
  const { activeWorkspace, refreshContext } = useWorkspace();

  const [name, setName] = useState(activeWorkspace?.name ?? '');
  const [isSaving, setIsSaving] = useState(false);
  const [savedName, setSavedName] = useState<string | null>(null);
  const [renameError, setRenameError] = useState<string | null>(null);

  const [options, setOptions] = useState<WorkspaceClosureOptions | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isClosing, setIsClosing] = useState(false);
  const [closureError, setClosureError] = useState<string | null>(null);

  // El nombre editable sigue al Workspace activo: cambiar de Workspace en el selector debe recargar
  // el formulario en vez de dejar escrito el nombre del anterior.
  useEffect(() => {
    setName(activeWorkspace?.name ?? '');
    setSavedName(null);
  }, [activeWorkspace?.id, activeWorkspace?.name]);

  const loadOptions = useCallback(async () => {
    try {
      setOptions(await lifecycle.getClosureOptions());
    } catch {
      // La zona de baja es secundaria: si no se puede resolver, el renombrado sigue funcionando.
      setOptions(null);
    }
  }, [lifecycle]);

  useEffect(() => {
    void loadOptions();
  }, [loadOptions, activeWorkspace?.id]);

  const submitRename = async (event: React.FormEvent) => {
    event.preventDefault();
    setRenameError(null);
    setSavedName(null);
    setIsSaving(true);

    try {
      const workspace = await lifecycle.rename(name);
      // Sin reemitir la sesión: solo se resincroniza el contexto para que el selector y la cabecera
      // muestren el nombre nuevo (CA-1). Las opciones de baja se recargan también: llevan el nombre
      // dentro y el diálogo de confirmación anunciaría el antiguo.
      await refreshContext();
      await loadOptions();
      setSavedName(workspace.name);
    } catch (error) {
      setRenameError(
        error instanceof HttpError ? error.message : 'No se pudo guardar el nombre. Inténtalo de nuevo.'
      );
    } finally {
      setIsSaving(false);
    }
  };

  const confirmClosure = async (decision: 'transfer' | 'delete', newOwnerUserId?: string) => {
    setClosureError(null);
    setIsClosing(true);

    try {
      const result =
        decision === 'transfer'
          ? await lifecycle.transferOwnership(newOwnerUserId!)
          : await lifecycle.close();

      await refreshContext();
      setIsModalOpen(false);

      if (result.outcome === 'deleted' || options?.mode === 'auto_transfer') {
        // Se ha dejado de tener acceso (o el Workspace ya no está): el destino lo decide el
        // contexto recién resuelto — otro Workspace o el onboarding.
        navigate('/app', { replace: true });
        return;
      }

      await loadOptions();
    } catch (error) {
      setClosureError(
        error instanceof HttpError ? error.message : 'No se pudo completar la operación. Inténtalo de nuevo.'
      );
    } finally {
      setIsClosing(false);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Ajustes del Workspace</h2>
        <p className="text-xs text-[#76786b]">
          Nombre de la explotación y qué hacer con ella cuando dejes de usarla.
        </p>
      </div>

      {/* Renombrar (HU-1/CA-1) */}
      <form
        onSubmit={submitRename}
        className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-4"
      >
        <h3 className="font-headline font-bold text-base text-[#1c1c19] border-b border-[#f0ede8] pb-2">
          Datos del Workspace
        </h3>

        <div className="space-y-1.5">
          <label
            htmlFor="workspace-name"
            className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
          >
            Nombre del Workspace
          </label>
          <input
            id="workspace-name"
            type="text"
            value={name}
            maxLength={120}
            onChange={(e) => setName(e.target.value)}
            className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19]"
          />
          <p className="text-[11px] text-[#76786b]">
            Lo verá todo el equipo. Cualquier miembro puede cambiarlo.
          </p>
        </div>

        {renameError && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {renameError}
          </p>
        )}

        {savedName && (
          <p
            role="status"
            className="p-3 bg-[#c9f16f] text-[#33450d] rounded-xl font-bold text-xs flex items-center gap-2"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">check_circle</span>
            <span>Ahora se llama «{savedName}».</span>
          </p>
        )}

        <div className="pt-2 flex justify-end">
          <button
            type="submit"
            disabled={isSaving || name.trim().length === 0 || name.trim() === activeWorkspace?.name}
            className="px-6 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isSaving ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </div>
      </form>

      {/* Propiedad y baja (HU-2/HU-3/HU-4) */}
      <section className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-4">
        <h3 className="font-headline font-bold text-base text-[#1c1c19] border-b border-[#f0ede8] pb-2">
          Propiedad y baja
        </h3>

        {options === null && (
          <p className="text-sm text-[#76786b]">Cargando las opciones del Workspace…</p>
        )}

        {options?.mode === 'not_owner' && (
          <p className="text-sm text-[#45483c]">
            Solo la persona propietaria puede dar de baja el Workspace o traspasar su propiedad. Si
            quieres dejar de formar parte, pídeselo desde{' '}
            <button
              type="button"
              onClick={() => navigate('/app/miembros')}
              className="font-semibold text-[#33450d] hover:underline"
            >
              Miembros y accesos
            </button>
            .
          </p>
        )}

        {options !== null && options.mode !== 'not_owner' && (
          <div className="space-y-3">
            <p className="text-sm text-[#45483c]">
              {options.mode === 'auto_transfer'
                ? `Este Workspace tiene ${options.active_owners} personas propietarias. Si te vas, pasará a ${options.successor_name} y seguirá funcionando.`
                : options.mode === 'choose'
                  ? 'Eres la única persona propietaria. Al irte tendrás que decidir si traspasas el Workspace a alguien del equipo o lo das de baja.'
                  : 'Eres la única persona en este Workspace. Puedes darlo de baja: no se borra nada y podrás volver a levantarlo.'}
            </p>

            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={() => {
                  setClosureError(null);
                  setIsModalOpen(true);
                }}
                className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl border border-[#ba1a1a] text-[#ba1a1a] hover:bg-[#fdf3f3] text-xs font-bold transition-colors"
              >
                <span className="material-symbols-outlined text-lg" aria-hidden="true">logout</span>
                {options.mode === 'auto_transfer' ? 'Salir y ceder mi propiedad' : 'Dar de baja el Workspace'}
              </button>

              <button
                type="button"
                onClick={() => navigate('/reactivations')}
                className="text-xs font-semibold text-[#33450d] hover:underline"
              >
                Workspaces dados de baja
              </button>
            </div>
          </div>
        )}
      </section>

      {isModalOpen && options !== null && (
        <CloseWorkspaceModal
          options={options}
          isBusy={isClosing}
          errorMessage={closureError}
          onCancel={() => setIsModalOpen(false)}
          onConfirm={confirmClosure}
        />
      )}
    </div>
  );
};
