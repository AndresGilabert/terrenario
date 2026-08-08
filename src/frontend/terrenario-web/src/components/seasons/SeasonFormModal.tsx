import React, { useEffect, useState } from 'react';
import type { Season } from '../../types/season.types';
import { Modal } from '../common/Modal';

export interface SeasonFormValues {
  name: string;
  start_date: string;
  end_date: string | null;
}

interface SeasonFormModalProps {
  isOpen: boolean;
  /** Temporada a editar; `null` para alta. */
  season: Season | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (values: SeasonFormValues) => void;
}

const NAME_MAX = 120;

/**
 * Alta y edición de una temporada (MVP-203). En alta, avisa de que la nueva pasará a ser la temporada
 * de trabajo del creador (MVP-209), sin desbancar a nadie. La fecha de fin es estimada y opcional y no
 * se bloquea por rango operativo (RN-023 es un aviso de las historias operativas, no del maestro).
 * Reutiliza el shell y la paleta del prototipo (`TemporadasView`/`OnboardingStep2`).
 */
export const SeasonFormModal: React.FC<SeasonFormModalProps> = ({
  isOpen,
  season,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const isEdit = season !== null;

  const [name, setName] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setName(season?.name ?? '');
    setStartDate(season?.start_date ?? '');
    setEndDate(season?.end_date ?? '');
    setLocalError(null);
  }, [isOpen, season]);

  if (!isOpen) return null;

  const canSubmit = name.trim().length > 0 && startDate.length > 0 && !isSubmitting;
  // MVP-209 — al crear, la nueva pasa a ser MI temporada de trabajo (por usuario), sin desbancar a
  // nadie ni cambiar el estado de las demás.
  const willBecomeWorking = !isEdit;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!name.trim()) {
      setLocalError('Escribe un nombre para la temporada.');
      return;
    }
    if (!startDate) {
      setLocalError('Indica la fecha de inicio de la temporada.');
      return;
    }
    if (endDate && endDate < startDate) {
      setLocalError('La fecha de fin no puede ser anterior a la de inicio.');
      return;
    }

    setLocalError(null);
    onSubmit({ name: name.trim(), start_date: startDate, end_date: endDate || null });
  };

  const shownError = localError ?? errorMessage;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Editar temporada' : 'Nueva temporada'}
      icon="calendar_today"
      closeDisabled={isSubmitting}
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm overflow-y-auto" noValidate>
        <div className="space-y-1.5">
          <label htmlFor="season-name" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Nombre de la temporada <span className="text-[#ba1a1a]">*</span>
          </label>
          <input
            id="season-name"
            type="text"
            required
            autoFocus
            maxLength={NAME_MAX}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="ej. Campaña Oliva 2026"
            disabled={isSubmitting}
            className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <label htmlFor="season-start" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Fecha de inicio <span className="text-[#ba1a1a]">*</span>
            </label>
            <input
              id="season-start"
              type="date"
              required
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="season-end" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Fecha fin <span className="normal-case text-[#76786b]">(estimada, opcional)</span>
            </label>
            <input
              id="season-end"
              type="date"
              value={endDate}
              min={startDate || undefined}
              onChange={(e) => setEndDate(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
          </div>
        </div>

        {willBecomeWorking && (
          <p className="text-[11px] text-[#33450d] bg-[#eef2e0] border border-[#d3dcae] rounded-lg px-2.5 py-1.5 flex items-center gap-1.5">
            <span className="material-symbols-outlined text-sm" aria-hidden="true">edit_note</span>
            Al crearla pasará a ser tu temporada de trabajo (la que se autoselecciona al registrar). No
            cambia la de tus compañeros.
          </p>
        )}

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
            <span>{isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear temporada'}</span>
            <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
          </button>
        </div>
      </form>
    </Modal>
  );
};
