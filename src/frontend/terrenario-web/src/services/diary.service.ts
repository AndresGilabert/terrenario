import type { HttpClient } from './http-client';
import type { DiaryFilters, DiaryListResponse } from '../types/diary.types';

/**
 * Servicio del diario cronológico unificado (MVP-305) sobre el cliente HTTP común (P-007).
 *
 * Solo lectura a propósito: cada registro se crea, corrige y elimina por el recurso al que pertenece
 * (`activity.service`, `purchase.service`, `consumption.service`), que es donde viven sus reglas. El
 * diario únicamente agrega.
 */
export function createDiaryService(http: HttpClient) {
  return {
    async listDiary(filters?: DiaryFilters): Promise<DiaryListResponse> {
      return http.request<DiaryListResponse>('/api/v1/diary', {
        query: {
          from: filters?.from,
          to: filters?.to,
          plot_id: filters?.plotId,
          season_id: filters?.seasonId,
          // `type` es repetible; con un solo valor basta el parámetro simple, que es el único caso
          // que usa hoy el filtro de la vista.
          type: filters?.types?.length === 1 ? filters.types[0] : undefined,
        },
      });
    },
  };
}

export type DiaryService = ReturnType<typeof createDiaryService>;
