import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiClient } from '../../contexts/ApiContext';
import { createWorkerService } from '../../services/worker.service';
import { createMemberService } from '../../services/member.service';
import { HttpError } from '../../services/http-client';
import type { CreateWorkerPayload, Worker } from '../../types/worker.types';
import type { WorkspacePerson } from '../../types/member.types';
import { WorkerFormModal } from './WorkerFormModal';

/**
 * Maestro de trabajadores del Workspace (MVP-204, HU-1/HU-2). Es el roster de responsables
 * seleccionables para las labores del diario. Reúne dos orígenes coherentes con RN-027:
 *  - Los **miembros del Workspace** aparecen automáticamente como seleccionables (CA-1), en modo
 *    lectura: su acceso se administra en «Miembros y accesos».
 *  - Los **trabajadores sin cuenta** (cuadrilla) se dan de alta, editan e inactivan aquí (CA-2/CA-3).
 */
export const TrabajadoresView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const workerService = useMemo(() => createWorkerService(http), [http]);
  const memberService = useMemo(() => createMemberService(http), [http]);

  const [workers, setWorkers] = useState<Worker[]>([]);
  const [members, setMembers] = useState<WorkspacePerson[]>([]);
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
      const [workerList, people] = await Promise.all([
        workerService.listWorkers(),
        memberService.listPeople(),
      ]);
      setWorkers(workerList);
      setMembers(people.data.filter((p) => p.kind === 'member' && p.status === 'activo'));
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudieron cargar los trabajadores.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [workerService, memberService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const visibleWorkers = useMemo(
    () => workers.filter((w) => showInactive || w.is_active),
    [workers, showInactive]
  );
  const activeCount = useMemo(() => workers.filter((w) => w.is_active).length, [workers]);
  const inactiveCount = workers.length - activeCount;

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
      if (editingWorker) {
        await workerService.updateWorker(editingWorker.id, {
          ...payload,
          is_active: editingWorker.is_active,
        });
      } else {
        await workerService.createWorker(payload);
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
      await workerService.updateWorker(worker.id, {
        name: worker.name,
        hourly_rate: worker.hourly_rate,
        is_active: !worker.is_active,
      });
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
        <button
          onClick={openCreate}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
          <span>Añadir trabajador</span>
        </button>
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
          {/* CA-1 — Miembros del Workspace, seleccionables automáticamente (RN-027), en lectura */}
          <section className="bg-white rounded-2xl border border-[#e5e2dd] p-5 space-y-3">
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">groups</span>
                <h3 className="font-headline font-bold text-base text-[#1c1c19]">Miembros del Workspace</h3>
              </div>
              <button
                onClick={() => navigate('/app/miembros')}
                className="text-xs font-semibold text-[#33450d] hover:underline flex items-center gap-1 shrink-0"
              >
                Administrar accesos
                <span className="material-symbols-outlined text-sm" aria-hidden="true">arrow_forward</span>
              </button>
            </div>
            <p className="text-xs text-[#76786b]">
              Aparecen como responsables seleccionables de forma automática. Su acceso se gestiona en
              «Miembros y accesos».
            </p>
            {members.length === 0 ? (
              <p className="text-xs text-[#76786b] italic">Todavía no hay miembros activos.</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {members.map((m) => (
                  <span
                    key={m.user_id}
                    className="inline-flex items-center gap-2 pl-1.5 pr-3 py-1 rounded-full bg-[#f6f3ee] border border-[#e5e2dd]"
                  >
                    <span className="w-6 h-6 rounded-full bg-[#33450d] text-white flex items-center justify-center text-[10px] font-bold">
                      {initials(m.name ?? m.email)}
                    </span>
                    <span className="text-xs font-semibold text-[#1c1c19]">{m.name ?? m.email}</span>
                    {m.is_self && <span className="text-[10px] text-[#76786b]">(tú)</span>}
                  </span>
                ))}
              </div>
            )}
          </section>

          {/* Trabajadores sin cuenta (cuadrilla) — CRUD */}
          <section className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">badge</span>
                Cuadrilla sin cuenta
              </h3>
              {inactiveCount > 0 && (
                <button
                  type="button"
                  onClick={() => setShowInactive((v) => !v)}
                  aria-pressed={showInactive}
                  className={`flex items-center gap-2 px-3.5 py-2 rounded-xl border text-xs font-semibold transition-colors ${
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
            </div>

            {workers.length === 0 ? (
              <EmptyWorkers onAdd={openCreate} />
            ) : visibleWorkers.length === 0 ? (
              <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
                No hay trabajadores activos. Activa el filtro para ver los inactivos.
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {visibleWorkers.map((worker) => (
                  <WorkerCard
                    key={worker.id}
                    worker={worker}
                    isBusy={busyWorkerId === worker.id}
                    onEdit={() => openEdit(worker)}
                    onToggleActive={() => void toggleActive(worker)}
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
}

const WorkerCard: React.FC<WorkerCardProps> = ({ worker, isBusy, onEdit, onToggleActive }) => (
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
        <div className="flex items-center gap-2 mt-0.5">
          <span
            className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
              worker.is_active ? 'bg-[#c9f16f] text-[#33450d]' : 'bg-[#e5e2dd] text-[#76786b]'
            }`}
          >
            {worker.is_active ? 'ACTIVO' : 'INACTIVO'}
          </span>
          {worker.hourly_rate != null && (
            <span className="text-xs text-[#76786b]">
              {worker.hourly_rate.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}/h
            </span>
          )}
        </div>
      </div>
    </div>

    <div className="pt-3 border-t border-[#f0ede8] flex items-center justify-between gap-2">
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
      <button
        onClick={onEdit}
        className="px-3.5 py-1.5 rounded-xl text-xs font-bold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] flex items-center gap-1"
      >
        <span className="material-symbols-outlined text-sm" aria-hidden="true">edit</span>
        Editar
      </button>
    </div>
  </div>
);
