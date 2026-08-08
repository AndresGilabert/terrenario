import React from 'react';
import { Modal } from './Modal';

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  /** Qué va a pasar exactamente, en una frase. */
  message: React.ReactNode;
  confirmLabel?: string;
  isBusy?: boolean;
  /** Error devuelto por el servidor al confirmar (p. ej. una regla de negocio que lo impide). */
  errorMessage?: string | null;
  onCancel: () => void;
  onConfirm: () => void;
}

/**
 * Confirmación explícita de una acción destructiva (MVP-305, RN-037).
 *
 * RN-037 exige que la UI pida confirmación **antes** de eliminar un registro operativo. Se resuelve
 * con un diálogo compartido y no con un `window.confirm` porque hace falta explicar *qué* se elimina
 * y *qué consecuencias* tiene —el texto lo pone quien lo abre— y porque el servidor puede rechazar la
 * operación con una regla de negocio (p. ej. una compra con imputaciones vivas, `MVP-304`), y ese
 * mensaje tiene que caber en el mismo sitio donde se está decidiendo.
 *
 * La acción destructiva **no** es la del botón por defecto: el foco inicial va a «Cancelar», de modo
 * que un `Intro` de más no borre nada.
 *
 * Desde `MVP-704` se apoya en {@link Modal}, que es quien atrapa el foco, apaga el fondo y cierra con
 * `Escape`. Aquí solo queda lo propio del diálogo: sin cabecera —el encabezado va dentro, junto al
 * icono de aviso— y sin cierre al pulsar fuera, porque un clic despistado no debe descartar una
 * decisión que se acaba de pedir de forma explícita.
 */
export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  title,
  message,
  confirmLabel = 'Eliminar',
  isBusy = false,
  errorMessage = null,
  onCancel,
  onConfirm,
}) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onCancel}
      title={title}
      header={null}
      panelClassName="max-w-sm"
      closeOnBackdrop={false}
      closeDisabled={isBusy}
    >
      <div className="p-6 space-y-3">
        <div className="w-12 h-12 rounded-2xl bg-[#ffdad6] text-[#ba1a1a] flex items-center justify-center">
          <span className="material-symbols-outlined text-2xl" aria-hidden="true">delete</span>
        </div>
        {/* Sin `id`: el nombre del diálogo lo pone `Modal` con `aria-label`, y un identificador fijo
            se duplicaría si alguna vez hubiera dos confirmaciones vivas a la vez. */}
        <h3 className="font-headline font-bold text-lg text-[#1c1c19]">{title}</h3>
        <div className="text-sm text-[#45483c] space-y-2">{message}</div>

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
          /* El foco arranca en «Cancelar»: un Intro de más no debe borrar nada. */
          autoFocus
          className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#e5e2dd] rounded-xl disabled:opacity-60"
        >
          Cancelar
        </button>
        <button
          type="button"
          onClick={onConfirm}
          disabled={isBusy}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#ba1a1a] hover:bg-[#93000a] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          <span>{isBusy ? 'Eliminando…' : confirmLabel}</span>
          <span className="material-symbols-outlined text-sm" aria-hidden="true">delete</span>
        </button>
      </div>
    </Modal>
  );
};
