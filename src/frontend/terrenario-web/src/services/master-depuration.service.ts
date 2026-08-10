import type { HttpClient } from './http-client';
import type { MasterMergeResult, MasterResource } from '../types/master.types';

/**
 * Depuración de maestros (MVP-806) sobre el cliente HTTP común: borrado de la ficha nunca usada y
 * fusión de dos fichas del mismo maestro.
 *
 * Es **un** servicio parametrizado por recurso y no cuatro métodos repartidos por los servicios de
 * cada maestro, porque el contrato es literalmente el mismo en los cuatro: `DELETE /{recurso}/{id}` y
 * `POST /{recurso}/{id}/merge`. Como el resto de recursos con ámbito de Workspace, el manejo de
 * 401/403 de scope vive en el cliente (P-007).
 */
export function createMasterDepurationService(http: HttpClient, resource: MasterResource) {
  return {
    /**
     * Borra físicamente una ficha sin uso. Responde 422 `BUSINESS_RULE_MASTER_IN_USE` —con la cifra
     * en el mensaje— si el servidor encuentra histórico, que es la comprobación que manda.
     */
    async deleteRecord(id: string): Promise<void> {
      await http.request<void>(`/api/v1/${resource}/${id}`, { method: 'DELETE' });
    },

    /** Fusiona `absorbedId` dentro de `survivorId`: el superviviente es el de la ruta. */
    async mergeRecords(survivorId: string, absorbedId: string): Promise<MasterMergeResult> {
      return http.request<MasterMergeResult>(`/api/v1/${resource}/${survivorId}/merge`, {
        method: 'POST',
        body: { absorbed_id: absorbedId },
      });
    },
  };
}

export type MasterDepurationService = ReturnType<typeof createMasterDepurationService>;
