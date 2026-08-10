import React, { useEffect, useMemo, useState } from 'react';
import { Modal } from './Modal';
import type { MasterRecordLike } from '../../types/master.types';

export interface MergeMasterDialogProps {
  isOpen: boolean;
  /** Cómo se llama una ficha de este maestro en el texto: «terreno», «temporada»… */
  kindLabel: string;
  /** Ficha desde la que se abrió el diálogo. */
  record: MasterRecordLike | null;
  /** El resto de fichas del maestro, candidatas a fusionarse con ella. */
  candidates: MasterRecordLike[];
  /**
   * Fichas que **no pueden desaparecer** en una fusión. Hoy solo aplica a los responsables con cuenta
   * (RN-036, MVP-208): su nombre lo fija su cuenta de Google y cada cuenta tiene una única fila por
   * Workspace, así que la que sobrevive es siempre la suya.
   */
  isProtected?: (record: MasterRecordLike) => boolean;
  /** Por qué esa ficha no puede desaparecer. Se muestra cuando el sentido queda fijado. */
  protectedReason?: string;
  isBusy: boolean;
  errorMessage: string | null;
  onCancel: () => void;
  onConfirm: (survivorId: string, absorbedId: string) => void;
}

/**
 * Fusión de dos fichas del mismo maestro (MVP-806, HU-2).
 *
 * Es un diálogo compartido por los cuatro maestros porque la decisión es la misma en todos: con qué
 * ficha se une, cuál sobrevive y cuántos registros van a cambiar de ficha. Lo único que cambia entre
 * maestros son las palabras y, en responsables, que el sentido puede venir fijado.
 *
 * Tres cosas que el diálogo tiene que dejar dichas antes de que nadie pulse:
 *
 * - **Cuál desaparece.** No se deduce del orden en que se eligieron: se nombra.
 * - **Cuántos registros se reapuntan** (CA-3). Sale de `usage_count` del listado, que es la misma
 *   cifra que el servidor usa para decidir. Si no se conoce, se dice que no se conoce en vez de
 *   escribir un cero que puede ser falso.
 * - **Que no hay vuelta atrás.** Deshacer una fusión está fuera de alcance, igual que el borrado de
 *   RN-037, y eso se avisa aquí y no después.
 */
export const MergeMasterDialog: React.FC<MergeMasterDialogProps> = ({
  isOpen,
  kindLabel,
  record,
  candidates,
  isProtected = () => false,
  protectedReason,
  isBusy,
  errorMessage,
  onCancel,
  onConfirm,
}) => {
  const [otherId, setOtherId] = useState('');
  /** Cuando ninguna de las dos está protegida, el usuario elige cuál se queda. */
  const [keepOther, setKeepOther] = useState(false);

  // Cada apertura arranca sin selección: heredar la de la vez anterior invitaría a confirmar una
  // fusión que no se ha mirado.
  useEffect(() => {
    if (isOpen) {
      setOtherId('');
      setKeepOther(false);
    }
  }, [isOpen, record?.id]);

  const other = useMemo(
    () => candidates.find((candidate) => candidate.id === otherId) ?? null,
    [candidates, otherId]
  );

  const recordProtected = record ? isProtected(record) : false;
  const otherProtected = other ? isProtected(other) : false;
  /** Dos fichas protegidas son dos personas distintas: fusionarlas borraría la ficha de una de ellas. */
  const bothProtected = recordProtected && otherProtected;
  /** El sentido lo fija la regla, no el usuario, en cuanto una de las dos está protegida. */
  const directionFixed = !bothProtected && (recordProtected || otherProtected);

  const survivor = !record || !other
    ? null
    : bothProtected
      ? null
      : otherProtected || (!recordProtected && keepOther)
        ? other
        : record;
  const absorbed = !survivor || !record || !other ? null : survivor.id === record.id ? other : record;

  const reassigned = absorbed?.usage_count;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onCancel}
      title={`Fusionar ${kindLabel}`}
      icon="merge"
      panelClassName="max-w-md"
      closeOnBackdrop={false}
      closeDisabled={isBusy}
    >
      <div className="p-6 space-y-4">
        <p className="text-sm text-[#45483c]">
          Une <strong>{record?.name}</strong> con otra ficha del maestro. Todo lo que tenga registrado
          la que desaparezca pasará a la que se conserve.
        </p>

        <div className="space-y-1.5">
          <label htmlFor="merge-other" className="text-xs font-bold text-[#45483c]">
            Fusionar con
          </label>
          <select
            id="merge-other"
            value={otherId}
            onChange={(event) => setOtherId(event.target.value)}
            disabled={isBusy}
            className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          >
            <option value="">Elige una ficha…</option>
            {candidates.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.name}
              </option>
            ))}
          </select>
        </div>

        {bothProtected && (
          <div role="alert" className="p-3 rounded-xl bg-[#fdf6e3] border border-[#f0e2b8] text-[#8a6d1a] text-sm">
            {protectedReason ?? 'Ninguna de las dos fichas puede desaparecer en una fusión.'}
          </div>
        )}

        {survivor && absorbed && (
          <div className="space-y-2 p-3 rounded-xl bg-[#f6f3ee] border border-[#e5e2dd] text-sm">
            <p className="text-[#45483c]">
              Se conserva <strong>{survivor.name}</strong> y desaparece{' '}
              <strong>{absorbed.name}</strong>.
            </p>
            <p className="text-xs text-[#76786b]">
              {reassigned == null
                ? 'Los registros de la ficha absorbida pasarán a la que se conserva.'
                : reassigned === 0
                  ? 'La ficha absorbida no tiene ningún registro que reapuntar.'
                  : `Se reapuntarán ${reassigned} ${reassigned === 1 ? 'registro' : 'registros'} a la ficha que se conserva.`}
            </p>

            {directionFixed ? (
              protectedReason && <p className="text-xs text-[#8a6d1a]">{protectedReason}</p>
            ) : (
              <button
                type="button"
                onClick={() => setKeepOther((value) => !value)}
                disabled={isBusy}
                className="text-xs font-semibold text-[#33450d] hover:underline flex items-center gap-1 disabled:opacity-60"
              >
                <span className="material-symbols-outlined text-sm" aria-hidden="true">swap_horiz</span>
                Conservar «{absorbed.name}» en su lugar
              </button>
            )}
          </div>
        )}

        <p className="text-xs text-[#8a6d1a] bg-[#fdf6e3] border border-[#f0e2b8] rounded-lg px-2.5 py-1.5 flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">warning</span>
          Una fusión no se puede deshacer.
        </p>

        {errorMessage && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </div>
        )}
      </div>

      <div className="px-6 py-4 bg-[#f6f3ee] border-t border-[#e5e2dd] flex items-center justify-end gap-3">
        <button
          type="button"
          onClick={onCancel}
          disabled={isBusy}
          className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#e5e2dd] rounded-xl disabled:opacity-60"
        >
          Cancelar
        </button>
        <button
          type="button"
          onClick={() => survivor && absorbed && onConfirm(survivor.id, absorbed.id)}
          disabled={isBusy || !survivor || !absorbed}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          <span>{isBusy ? 'Fusionando…' : 'Fusionar'}</span>
          <span className="material-symbols-outlined text-sm" aria-hidden="true">merge</span>
        </button>
      </div>
    </Modal>
  );
};
