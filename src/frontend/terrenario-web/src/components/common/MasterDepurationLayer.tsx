import React from 'react';
import { ConfirmDialog } from './ConfirmDialog';
import { MergeMasterDialog } from './MergeMasterDialog';
import type { useMasterDepuration } from '../../lib/use-master-depuration';
import type { MasterRecordLike } from '../../types/master.types';

interface MasterDepurationLayerProps {
  /** Cómo se nombra una ficha de este maestro: «el terreno», «la tarea»… con artículo. */
  kindLabel: string;
  /** Sin artículo, para el título del diálogo de fusión: «terrenos», «tareas»… */
  kindPlural: string;
  depuration: ReturnType<typeof useMasterDepuration>;
  /** Las demás fichas del maestro con las que la seleccionada puede fusionarse. */
  candidates: MasterRecordLike[];
  isProtected?: (record: MasterRecordLike) => boolean;
  protectedReason?: string;
}

/**
 * MVP-806 — Todo lo que la depuración de un maestro pinta fuera de sus tarjetas: el aviso de lo que
 * acaba de pasar, la confirmación del borrado y el diálogo de fusión.
 *
 * Un único componente para los cuatro maestros. Lo que cada vista aporta es dónde van los botones que
 * disparan las acciones —eso sí depende de su maqueta— y las palabras; el resto es idéntico, y con
 * cuatro copias la que se quedaría atrás sería siempre la misma.
 */
export const MasterDepurationLayer: React.FC<MasterDepurationLayerProps> = ({
  kindLabel,
  kindPlural,
  depuration,
  candidates,
  isProtected,
  protectedReason,
}) => (
  <>
    {depuration.notice && (
      <div
        role="status"
        className="p-3 rounded-xl bg-[#eef2e0] border border-[#c9f16f] text-[#33450d] text-sm flex items-start justify-between gap-3"
      >
        <span className="flex items-start gap-2">
          <span className="material-symbols-outlined text-base" aria-hidden="true">check_circle</span>
          {depuration.notice}
        </span>
        <button
          type="button"
          onClick={depuration.dismissNotice}
          className="text-xs font-semibold hover:underline shrink-0"
        >
          Cerrar
        </button>
      </div>
    )}

    <ConfirmDialog
      isOpen={depuration.deleting !== null}
      title={`Eliminar ${kindLabel}`}
      isBusy={depuration.isBusy}
      errorMessage={depuration.merging ? null : depuration.error}
      onCancel={depuration.cancel}
      onConfirm={depuration.confirmDelete}
      message={
        <>
          <p>
            Se eliminará <strong>{depuration.deleting?.name}</strong> de forma definitiva.
          </p>
          {/* Que se pueda borrar significa que nunca se usó: decirlo quita el miedo a perder
              histórico, que es exactamente lo que la inactivación existe para evitar (RN-037). */}
          <p className="text-xs text-[#76786b]">
            Nunca se ha usado en ningún registro, así que tu histórico no cambia. No se puede deshacer.
          </p>
        </>
      }
    />

    <MergeMasterDialog
      isOpen={depuration.merging !== null}
      kindLabel={kindPlural}
      record={depuration.merging}
      candidates={candidates}
      isProtected={isProtected}
      protectedReason={protectedReason}
      isBusy={depuration.isBusy}
      errorMessage={depuration.error}
      onCancel={depuration.cancel}
      onConfirm={depuration.confirmMerge}
    />
  </>
);
