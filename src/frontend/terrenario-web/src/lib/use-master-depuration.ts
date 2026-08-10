import { useCallback, useMemo, useState } from 'react';
import { useApiClient } from '../contexts/ApiContext';
import { createMasterDepurationService } from '../services/master-depuration.service';
import { HttpError } from '../services/http-client';
import type { MasterRecordLike, MasterResource } from '../types/master.types';

interface UseMasterDepurationOptions {
  /** Se invoca tras una operación correcta para que la vista vuelva a leer su maestro. */
  onChanged: () => Promise<void> | void;
}

/**
 * Estado de las dos acciones de depuración de un maestro (MVP-806): borrar y fusionar.
 *
 * Vive en un hook compartido y no en cada vista porque las cuatro necesitan exactamente lo mismo
 * —qué ficha se está confirmando, si hay una operación en curso, el error del servidor y el aviso de
 * lo que pasó— y porque el error del servidor es la parte que **no** se puede reescribir en cliente:
 * el 422 de uso llega con la cifra y el desglose ya redactados, y mostrarlo tal cual es lo que hace
 * que el usuario sepa dónde mirar.
 *
 * Lo que el hook deliberadamente **no** hace es decidir si una ficha es borrable. Eso lo dice
 * `usage_count` del listado (`isDeletable`), y la palabra definitiva la tiene el servidor.
 */
export function useMasterDepuration(
  resource: MasterResource,
  { onChanged }: UseMasterDepurationOptions
) {
  const http = useApiClient();
  const service = useMemo(() => createMasterDepurationService(http, resource), [http, resource]);

  const [deleting, setDeleting] = useState<MasterRecordLike | null>(null);
  const [merging, setMerging] = useState<MasterRecordLike | null>(null);
  const [isBusy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const askDelete = useCallback((record: MasterRecordLike) => {
    setDeleting(record);
    setError(null);
  }, []);

  const askMerge = useCallback((record: MasterRecordLike) => {
    setMerging(record);
    setError(null);
  }, []);

  const cancel = useCallback(() => {
    if (isBusy) return;
    setDeleting(null);
    setMerging(null);
    setError(null);
  }, [isBusy]);

  const run = useCallback(
    async (operation: () => Promise<string>, fallback: string) => {
      setBusy(true);
      setError(null);
      try {
        const message = await operation();
        setDeleting(null);
        setMerging(null);
        setNotice(message);
        await onChanged();
      } catch (cause) {
        // El mensaje del servidor se muestra tal cual: trae la cifra de registros que impiden la
        // operación, que es justo lo que el CA-2 pide y lo que el cliente no puede inventarse.
        setError(cause instanceof HttpError ? cause.message : fallback);
      } finally {
        setBusy(false);
      }
    },
    [onChanged]
  );

  const confirmDelete = useCallback(() => {
    if (!deleting) return;
    const { id, name } = deleting;
    void run(async () => {
      await service.deleteRecord(id);
      return `«${name}» se ha eliminado.`;
    }, 'No se pudo eliminar la ficha. Inténtalo de nuevo.');
  }, [deleting, run, service]);

  const confirmMerge = useCallback(
    (survivorId: string, absorbedId: string) => {
      void run(async () => {
        const result = await service.mergeRecords(survivorId, absorbedId);
        return result.reassigned_count === 0
          ? `«${result.absorbed_name}» se ha fusionado con «${result.survivor_name}».`
          : `«${result.absorbed_name}» se ha fusionado con «${result.survivor_name}»: ` +
            `${result.reassigned_count} ${result.reassigned_count === 1 ? 'registro' : 'registros'} reapuntados.`;
      }, 'No se pudo fusionar. Inténtalo de nuevo.');
    },
    [run, service]
  );

  return {
    deleting,
    merging,
    isBusy,
    error,
    notice,
    askDelete,
    askMerge,
    cancel,
    confirmDelete,
    confirmMerge,
    dismissNotice: useCallback(() => setNotice(null), []),
  };
}
