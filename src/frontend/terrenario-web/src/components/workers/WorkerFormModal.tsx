import React, { useEffect, useState } from 'react';
import type { CreateWorkerPayload, Worker } from '../../types/worker.types';
import { Modal } from '../common/Modal';

interface WorkerFormModalProps {
  isOpen: boolean;
  /** Trabajador a editar; `null` para alta. */
  worker: Worker | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (payload: CreateWorkerPayload) => void;
}

const NAME_MAX = 150;

/**
 * Alta y edición de un responsable (MVP-204 · MVP-208). Solo el nombre es obligatorio (CA-2); la
 * tarifa horaria es opcional y de referencia (no automatiza el coste, RN-003). No se piden rol ni
 * teléfono: no están en el modelo de datos de la KB (el prototipo los mostraba, pero es solo
 * referencia visual).
 *
 * Al editar un **miembro del Workspace** el nombre se muestra en lectura y solo se guarda la tarifa
 * (CA-4): su nombre llega de su cuenta de Google (RN-036). Se enseña igualmente, en vez de ocultarlo,
 * para que quede claro a quién se le está poniendo la tarifa.
 */
export const WorkerFormModal: React.FC<WorkerFormModalProps> = ({
  isOpen,
  worker,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const isEdit = worker !== null;
  const isMember = worker?.kind === 'member';

  const [name, setName] = useState('');
  const [hourlyRate, setHourlyRate] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setName(worker?.name ?? '');
    setHourlyRate(worker?.hourly_rate != null ? String(worker.hourly_rate) : '');
    setLocalError(null);
  }, [isOpen, worker]);

  if (!isOpen) return null;

  const canSubmit = name.trim().length > 0 && !isSubmitting;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!name.trim()) {
      setLocalError('Escribe un nombre para el trabajador.');
      return;
    }

    let parsedRate: number | null = null;
    if (hourlyRate.trim().length > 0) {
      const value = Number(hourlyRate);
      if (!Number.isFinite(value) || value < 0) {
        setLocalError('La tarifa horaria debe ser un número igual o mayor que 0.');
        return;
      }
      parsedRate = value;
    }

    setLocalError(null);
    onSubmit({ name: name.trim(), hourly_rate: parsedRate });
  };

  const shownError = localError ?? errorMessage;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isMember ? 'Tarifa del miembro' : isEdit ? 'Editar trabajador' : 'Añadir trabajador'}
      icon="badge"
      panelClassName="max-w-md"
      closeDisabled={isSubmitting}
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm overflow-y-auto" noValidate>
        <div className="space-y-1.5">
          <label htmlFor="worker-name" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Nombre y apellidos {!isMember && <span className="text-[#ba1a1a]">*</span>}
          </label>
          <input
            id="worker-name"
            type="text"
            required={!isMember}
            autoFocus={!isMember}
            readOnly={isMember}
            maxLength={NAME_MAX}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="ej. Antonio García"
            disabled={isSubmitting}
            className={`w-full px-3.5 py-2.5 border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none disabled:opacity-60 ${
              isMember
                ? 'bg-[#f0ede8] text-[#76786b] cursor-not-allowed'
                : 'bg-[#f6f3ee] focus:border-[#33450d] focus:bg-white'
            }`}
          />
          {isMember && (
            <p className="text-[11px] text-[#76786b] flex items-center gap-1.5">
              <span className="material-symbols-outlined text-sm" aria-hidden="true">lock</span>
              Llega de su cuenta de Google: se actualiza solo cuando la persona lo cambia allí.
            </p>
          )}
        </div>

        <div className="space-y-1.5">
          <label htmlFor="worker-rate" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Tarifa por hora (€)
          </label>
          <input
            id="worker-rate"
            type="number"
            min={0}
            step="0.01"
            value={hourlyRate}
            onChange={(e) => setHourlyRate(e.target.value)}
            placeholder="ej. 12.50"
            autoFocus={isMember}
            disabled={isSubmitting}
            className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          />
          <p className="text-[11px] text-[#76786b] flex items-center gap-1.5">
            <span className="material-symbols-outlined text-sm" aria-hidden="true">info</span>
            Opcional y solo de referencia: el coste de cada labor se registra a mano.
          </p>
        </div>

        {shownError && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {shownError}
          </div>
        )}

        <div className="pt-3 flex items-center justify-end gap-3 border-t border-[#e5e2dd]">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl disabled:opacity-60"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={!canSubmit}
            className="flex items-center gap-2 px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <span>
              {isSubmitting
                ? 'Guardando…'
                : isMember
                  ? 'Guardar tarifa'
                  : isEdit
                    ? 'Guardar cambios'
                    : 'Añadir trabajador'}
            </span>
            <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
          </button>
        </div>
      </form>
    </Modal>
  );
};
