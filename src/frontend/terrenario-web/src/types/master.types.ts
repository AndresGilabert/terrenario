/**
 * Depuración de maestros (MVP-806): lo común a terrenos, temporadas, trabajadores y tareas.
 *
 * Los cuatro maestros comparten exactamente el mismo par de acciones —borrar la ficha que nunca se
 * usó y fusionar dos que son la misma cosa— y exactamente el mismo contrato. Repetirlo cuatro veces
 * sería garantizar que la quinta pantalla lo hiciera distinto.
 */

/** Los cuatro recursos con depuración. El valor es el segmento de la ruta de API. */
export type MasterResource = 'plots' | 'seasons' | 'workers' | 'tasks';

/**
 * Lo que la depuración necesita de una ficha, venga del maestro que venga. Cada maestro tiene su
 * propio tipo completo; esto es la parte que comparten.
 */
export interface MasterRecordLike {
  id: string;
  name: string;
  /**
   * Cuántos registros la referencian (MVP-806, CA-2). `null` o ausente significa **«no consultado»**,
   * no «ninguno»: solo lo trae el listado. Ante la duda no se ofrece borrar, porque ofrecer un
   * borrado que el servidor va a rechazar es peor que no ofrecerlo.
   */
  usage_count?: number | null;
}

/** Resultado de `POST /{maestro}/{id}/merge`. */
export interface MasterMergeResult {
  survivor_id: string;
  survivor_name: string;
  absorbed_id: string;
  absorbed_name: string;
  /** Cuántos registros operativos cambiaron de ficha. Es la cifra que confirma la fusión (CA-3). */
  reassigned_count: number;
}

/** ¿Se puede ofrecer «Eliminar» sobre esta ficha? Solo con un «sin uso» **confirmado** por el servidor. */
export function isDeletable(record: MasterRecordLike): boolean {
  return record.usage_count === 0;
}
