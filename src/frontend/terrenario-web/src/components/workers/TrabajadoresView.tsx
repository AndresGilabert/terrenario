import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { createWorkerService } from '../../services/worker.service';
import { HttpError } from '../../services/http-client';
import type { CreateWorkerPayload, Worker } from '../../types/worker.types';
import { WorkerFormModal } from './WorkerFormModal';

/**
 * Maestro de responsables del Workspace (MVP-204, HU-1/HU-2 · MVP-208, HU-1/HU-2). Es el roster de
 * personas asignables a las labores del diario y se construye desde **un solo listado**
 * (`GET /workers`, CA-2): antes combinaba en cliente dos endpoints con identificadores distintos, y
 * un miembro elegido como responsable no se podía guardar (P-034).
 *
 * Las dos clases de persona se siguen distinguiendo en pantalla, porque lo que se puede hacer con
 * cada una es distinto:
 *  - **Miembros del Workspace** (`kind: 'member'`): entran solos al aceptar la invitación (RN-027).
 *    Su nombre llega de su cuenta de Google (RN-036) y su disponibilidad la gobierna el acceso, que
 *    se administra en «Miembros y accesos». Aquí solo se ajusta su tarifa horaria.
 *  - **Cuadrilla sin cuenta** (`kind: 'crew'`): se da de alta, edita e inactiva aquí (CA-2/CA-3).
 */
export const TrabajadoresView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const workerService = useMemo(() => createWorkerService(http), [http]);

  const [workers, setWorkers] = useState<Worker[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [showInactive, setShowInactive] = useState(false);

  const [isModalOpen, setModalOpen] = useState(false);
  const [editingWorker, setEditingWorker] = useState<Worker | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [busyWorkerId, setBusyWorkerId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      setWorkers(await workerService.listWorkers());
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudieron cargar los trabajadores.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [workerService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const members = useMemo(() => workers.filter((w) => w.kind === 'member'), [workers]);
  const crew = useMemo(() => workers.filter((w) => w.kind === 'crew'), [workers]);
  const inactiveCount = useMemo(() => workers.filter((w) => !w.is_active).length, [workers]);

  const visible = useCallback(
    (list: Worker[]) => list.filter((w) => showInactive || w.is_active),
    [showInactive]
  );
  const visibleMembers = visible(members);
  const visibleCrew = visible(crew);

  const openCreate = () => {
    setEditingWorker(null);
    setSubmitError(null);
    setModalOpen(true);
  };

  const openEdit = (worker: Worker) => {
    setEditingWorker(worker);
    setSubmitError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (payload: CreateWorkerPayload) => {
    setSubmitting(true);
    setSubmitError(null);
    try {
      if (!editingWorker) {
        await workerService.createWorker(payload);
      } else if (editingWorker.kind === 'member') {
        // De un miembro solo se edita la tarifa: enviar su nombre respondería 422 (CA-4).
        await workerService.updateWorker(editingWorker.id, { hourly_rate: payload.hourly_rate });
      } else {
        await workerService.updateWorker(editingWorker.id, payload);
      }
      setModalOpen(false);
      setEditingWorker(null);
      await reload();
    } catch (error) {
      setSubmitError(
        error instanceof HttpError ? error.message : 'No se pudo guardar el trabajador. Inténtalo de nuevo.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  const toggleActive = async (worker: Worker) => {
    setBusyWorkerId(worker.id);
    try {
      await workerService.updateWorker(worker.id, { is_active: !worker.is_active });
      await reload();
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cambiar el estado del trabajador.'
      );
    } finally {
      setBusyWorkerId(null);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Trabajadores</h2>
          <p className="text-xs text-[#76786b]">
            Personas asignables como responsables de las labores del diario. Incluye a los miembros del
            Workspace y a la cuadrilla sin cuenta.
          </p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {inactiveCount > 0 && (
            <button
              type="button"
              onClick={() => setShowInactive((v) => !v)}
              aria-pressed={showInactive}
              className={`flex items-center gap-2 px-3.5 py-2.5 rounded-xl border text-xs font-semibold transition-colors ${
                showInactive
                  ? 'bg-[#33450d] text-white border-[#33450d]'
                  : 'bg-white text-[#45483c] border-[#e5e2dd] hover:bg-[#f0ede8]'
              }`}
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">
                {showInactive ? 'visibility' : 'visibility_off'}
              </span>
              <span>Inactivos ({inactiveCount})</span>
            </button>
          )}
          <button
            onClick={openCreate}
            className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
            <span>Añadir trabajador</span>
          </button>
        </div>
      </div>

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <>
          {/* Miembros del Workspace: están en el maestro por RN-027, no se dan de alta a mano */}
          <section className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">groups</span>
                Miembros del Workspace
              </h3>
              <button
                onClick={() => navigate('/app/miembros')}
                className="text-xs font-semibold text-[#33450d] hover:underline flex items-center gap-1 shrink-0"
              >
                Administrar accesos
                <span className="material-symbols-outlined text-sm" aria-hidden="true">arrow_forward</span>
              </button>
            </div>
            <p className="text-xs text-[#76786b]">
              Aparecen aquí en cuanto aceptan la invitación. Su nombre llega de su cuenta de Google y su
              disponibilidad depende de su acceso: para retirar a alguien, hazlo en «Miembros y accesos».
            </p>
            {visibleMembers.length === 0 ? (
              <p className="text-xs text-[#76786b] italic">
                {members.length === 0
                  ? 'Todavía no hay miembros en el Workspace.'
                  : 'Ningún miembro con acceso. Activa el filtro para ver los que lo perdieron.'}
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {visibleMembers.map((worker) => (
                  <WorkerCard
                    key={worker.id}
                    worker={worker}
                    isBusy={busyWorkerId === worker.id}
                    onEdit={() => openEdit(worker)}
                    onToggleActive={() => void toggleActive(worker)}
                    onManageAccess={() => navigate('/app/miembros')}
                  />
                ))}
              </div>
            )}
          </section>

          {/* Trabajadores sin cuenta (cuadrilla) — CRUD */}
          <section className="space-y-3">
            <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
              <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">badge</span>
              Cuadrilla sin cuenta
            </h3>

            {crew.length === 0 ? (
              <EmptyWorkers onAdd={openCreate} />
            ) : visibleCrew.length === 0 ? (
              <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
                No hay trabajadores activos en la cuadrilla. Activa el filtro para ver los inactivos.
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {visibleCrew.map((worker) => (
                  <WorkerCard
                    key={worker.id}
                    worker={worker}
                    isBusy={busyWorkerId === worker.id}
                    onEdit={() => openEdit(worker)}
                    onToggleActive={() => void toggleActive(worker)}
                    onManageAccess={() => navigate('/app/miembros')}
                  />
                ))}
              </div>
            )}
          </section>
        </>
      )}

      <WorkerFormModal
        isOpen={isModalOpen}
        worker={editingWorker}
        isSubmitting={isSubmitting}
        errorMessage={submitError}
        onClose={() => {
          if (!isSubmitting) {
            setModalOpen(false);
            setEditingWorker(null);
          }
        }}
        onSubmit={(payload) => void handleSubmit(payload)}
      />
    </div>
  );
};

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((p) => p.charAt(0).toUpperCase()).join('') || '?';
}

const EmptyWorkers: React.FC<{ onAdd: () => void }> = ({ onAdd }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4">
    <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-3xl" aria-hidden="true">badge</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Aún no hay cuadrilla</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        Da de alta a los jornaleros y operarios sin cuenta para poder asignarlos como responsables de
        las labores del diario.
      </p>
    </div>
    <button
      onClick={onAdd}
      className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
    >
      <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
      Añadir mi primer trabajador
    </button>
  </div>
);

interface WorkerCardProps {
  worker: Worker;
  isBusy: boolean;
  onEdit: () => void;
  onToggleActive: () => void;
  onManageAccess: () => void;
}

const WorkerCard: React.FC<WorkerCardProps> = ({
  worker,
  isBusy,
  onEdit,
  onToggleActive,
  onManageAccess,
}) => {
  const isMember = worker.kind === 'member';

  return (
    <div
      className={`bg-white rounded-2xl border p-5 flex flex-col justify-between gap-4 shadow-2xs transition-all ${
        worker.is_active ? 'border-[#e5e2dd]' : 'border-[#dcd9d2] bg-[#faf8f4] opacity-90'
      }`}
    >
      <div className="flex items-center gap-3">
        <div className="w-11 h-11 rounded-full bg-[#33450d] text-white flex items-center justify-center text-sm font-bold shrink-0">
          {initials(worker.name)}
        </div>
        <div className="min-w-0">
          <h3 className="font-headline font-bold text-base text-[#1c1c19] truncate">{worker.name}</h3>
          <div className="flex items-center gap-2 mt-0.5 flex-wrap">
            <span
              className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                worker.is_active ? 'bg-[#c9f16f] text-[#33450d]' : 'bg-[#e5e2dd] text-[#76786b]'
              }`}
            >
              {worker.is_active ? 'ACTIVO' : isMember ? 'SIN ACCESO' : 'INACTIVO'}
            </span>
            {isMember && (
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#eef2e0] text-[#33450d]">
                MIEMBRO
              </span>
            )}
            {worker.hourly_rate != null && (
              <span className="text-xs text-[#76786b]">
                {worker.hourly_rate.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}/h
              </span>
            )}
          </div>
        </div>
      </div>

      <div className="pt-3 border-t border-[#f0ede8] flex items-center justify-between gap-2">
        {isMember ? (
          // RN-027: un miembro no se inactiva a mano. La vía de retirarlo es su acceso, así que el
          // hueco de «Inactivar» lleva a donde eso se decide en vez de ofrecer una acción que falla.
          <button
            onClick={onManageAccess}
            className="text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] flex items-center gap-1"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">badge</span>
            Gestionar acceso
          </button>
        ) : (
          <button
            onClick={onToggleActive}
            disabled={isBusy}
            className="text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-50 flex items-center gap-1"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">
              {worker.is_active ? 'toggle_off' : 'toggle_on'}
            </span>
            {worker.is_active ? 'Inactivar' : 'Reactivar'}
          </button>
        )}
        <button
          onClick={onEdit}
          className="px-3.5 py-1.5 rounded-xl text-xs font-bold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] flex items-center gap-1"
        >
          <span className="material-symbols-outlined text-sm" aria-hidden="true">edit</span>
          {isMember ? 'Editar tarifa' : 'Editar'}
        </button>
      </div>
    </div>
  );
};
