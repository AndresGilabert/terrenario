import { useMemo } from 'react';
import type { DiaryEntryType } from '../types/diary.types';
import { FILTER_ALL, useListUrlState } from './list-url-state';

/** Valor de un filtro de lista cuando no acota nada. No viaja a la URL. */
export const DIARY_FILTER_ALL = FILTER_ALL;

export interface DiaryUrlState {
  /** Tipo de registro; `todos` si no se acota. */
  type: DiaryEntryType | typeof DIARY_FILTER_ALL;
  plotId: string;
  workerId: string;
  /** Elección de temporada: `''` (defecto del servidor), `all`, o un identificador (MVP-701). */
  seasonSelection: string;
  /** Término **ya aplicado**. Lo que se está tecleando lo guarda la vista. */
  search: string;
  page: number;
  /** ¿Hay algo puesto a mano? Lo usa el rótulo de vacío y el botón de quitar filtros. */
  hasFilters: boolean;

  /**
   * Cambia un filtro: vuelve a la página 1 y **deja entrada de historial**.
   *
   * MVP-801 — Con `{ replace: true }` la **sustituye**. Lo usa la corrección de un ámbito que el
   * servidor no ha podido aplicar (`P-108`): no es una navegación del usuario, y dejar entrada haría
   * que «atrás» devolviera a la URL con el ámbito ajeno y volviera a corregirse en bucle.
   */
  setFilter: (patch: Partial<DiaryFilterPatch>, options?: { replace?: boolean }) => void;
  /** Cambia de página, con entrada de historial: «atrás» devuelve a la anterior. */
  setPage: (page: number) => void;
  /**
   * Fija el término ya rebotado. **Sustituye** la entrada de historial en vez de añadir una: si no,
   * escribir «sulfatado» dejaría una entrada por pulsación y el botón «atrás» quedaría inservible.
   */
  setSearch: (term: string) => void;
  clearFilters: () => void;
}

export interface DiaryFilterPatch {
  type: string;
  plotId: string;
  workerId: string;
  seasonSelection: string;
}

/**
 * Declaración de los parámetros del diario. Los nombres coinciden con los de la API para que la URL se
 * lea sola.
 *
 * La temporada tiene `''` por defecto y no `todos`: desde `MVP-701` el defecto lo resuelve el servidor
 * (RN-008), y `all` es una elección explícita con valor propio.
 */
const DIARY_SPEC = {
  filters: {
    type: { param: 'type', fallback: FILTER_ALL },
    plotId: { param: 'plot_id', fallback: FILTER_ALL },
    workerId: { param: 'worker_id', fallback: FILTER_ALL },
    seasonSelection: { param: 'season_id', fallback: '' },
  },
  search: 'search',
  page: 'page',
} as const;

/**
 * MVP-705 (`P-072`) — Estado de navegación del diario sobre la pieza común de `list-url-state`, que
 * desde `MVP-802` comparten las cuatro vistas operativas.
 */
export function useDiaryUrlState(): DiaryUrlState {
  const url = useListUrlState(DIARY_SPEC);
  const { values, search, page, hasFilters, setFilter, setPage, setSearch, clearFilters } = url;

  return useMemo(
    () => ({
      type: values.type as DiaryUrlState['type'],
      plotId: values.plotId,
      workerId: values.workerId,
      seasonSelection: values.seasonSelection,
      search,
      page,
      hasFilters,
      setFilter,
      setPage,
      setSearch,
      clearFilters,
    }),
    [values, search, page, hasFilters, setFilter, setPage, setSearch, clearFilters]
  );
}
