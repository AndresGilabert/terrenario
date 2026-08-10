import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import type { DiaryEntryType } from '../types/diary.types';

/** Valor de un filtro de lista cuando no acota nada. No viaja a la URL. */
export const DIARY_FILTER_ALL = 'todos';

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

/** Nombres de los parámetros. Coinciden con los de la API para que la URL se lea sola. */
const PARAM = {
  type: 'type',
  plotId: 'plot_id',
  workerId: 'worker_id',
  seasonId: 'season_id',
  search: 'search',
  page: 'page',
} as const;

/**
 * MVP-705 (`P-072`) — Estado de navegación del diario, con la **URL como fuente única** (RN-007).
 *
 * No es una regla nueva: `RN-007` ya exigía conservar los filtros en la recarga, y `MVP-405` la
 * materializó en la URL para el dashboard. Lo que hace esta historia es aplicarla a la vista que más
 * la necesita —el diario tiene cinco filtros y paginación— y que se quedó fuera.
 *
 * Dos invariantes sostienen el diseño:
 *
 * 1. **Los valores por defecto no ensucian la URL** (CA-5). `todos`, la página 1 y la búsqueda vacía se
 *    **borran** del parámetro en vez de escribirse. Y la temporada por defecto tampoco aparece: desde
 *    `MVP-701` la resuelve el servidor (RN-008), así que fijarla en la URL congelaría la campaña de
 *    trabajo del día en que se compartió el enlace.
 * 2. **La búsqueda sustituye la entrada de historial; el resto la añade.** Es lo que permite que
 *    «atrás» devuelva al estado anterior de filtros sin que teclear genere una entrada por carácter.
 */
export function useDiaryUrlState(): DiaryUrlState {
  const [searchParams, setSearchParams] = useSearchParams();

  const type = (searchParams.get(PARAM.type) ?? DIARY_FILTER_ALL) as DiaryUrlState['type'];
  const plotId = searchParams.get(PARAM.plotId) ?? DIARY_FILTER_ALL;
  const workerId = searchParams.get(PARAM.workerId) ?? DIARY_FILTER_ALL;
  const seasonSelection = searchParams.get(PARAM.seasonId) ?? '';
  const search = searchParams.get(PARAM.search) ?? '';

  // Una página que no es un entero positivo es basura en la URL, no un error del usuario: se cae a la
  // primera en vez de pedir una página −3 al servidor.
  const rawPage = Number(searchParams.get(PARAM.page));
  const page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1;

  /** Escribe la URL borrando lo que vale su defecto. `null` borra. */
  const write = useCallback(
    (patch: Record<string, string | null>, { replace }: { replace: boolean }) => {
      setSearchParams(
        (current) => {
          const next = new URLSearchParams(current);
          for (const [key, value] of Object.entries(patch)) {
            if (value === null || value === '') next.delete(key);
            else next.set(key, value);
          }
          return next;
        },
        { replace }
      );
    },
    [setSearchParams]
  );

  const setFilter = useCallback(
    (patch: Partial<DiaryFilterPatch>, options?: { replace?: boolean }) => {
      const next: Record<string, string | null> = {};
      if ('type' in patch) next[PARAM.type] = patch.type === DIARY_FILTER_ALL ? null : (patch.type ?? null);
      if ('plotId' in patch)
        next[PARAM.plotId] = patch.plotId === DIARY_FILTER_ALL ? null : (patch.plotId ?? null);
      if ('workerId' in patch)
        next[PARAM.workerId] = patch.workerId === DIARY_FILTER_ALL ? null : (patch.workerId ?? null);
      if ('seasonSelection' in patch) next[PARAM.seasonId] = patch.seasonSelection || null;

      // Cualquier cambio de filtro vuelve a la primera página: seguir en la 4 de un diario que acaba
      // de reducirse a 12 entradas dejaría la pantalla vacía sin explicar por qué. Va en la **misma**
      // escritura que el filtro, no en un efecto aparte, para que no salga una petición intermedia
      // con el filtro nuevo y la página vieja.
      next[PARAM.page] = null;

      write(next, { replace: options?.replace ?? false });
    },
    [write]
  );

  const setPage = useCallback(
    (value: number) => write({ [PARAM.page]: value <= 1 ? null : String(value) }, { replace: false }),
    [write]
  );

  const setSearch = useCallback(
    (term: string) => write({ [PARAM.search]: term || null, [PARAM.page]: null }, { replace: true }),
    [write]
  );

  const clearFilters = useCallback(
    () =>
      write(
        {
          [PARAM.type]: null,
          [PARAM.plotId]: null,
          [PARAM.workerId]: null,
          [PARAM.seasonId]: null,
          [PARAM.search]: null,
          [PARAM.page]: null,
        },
        { replace: false }
      ),
    [write]
  );

  const hasFilters =
    type !== DIARY_FILTER_ALL ||
    plotId !== DIARY_FILTER_ALL ||
    workerId !== DIARY_FILTER_ALL ||
    seasonSelection !== '' ||
    search !== '';

  return useMemo(
    () => ({
      type,
      plotId,
      workerId,
      seasonSelection,
      search,
      page,
      hasFilters,
      setFilter,
      setPage,
      setSearch,
      clearFilters,
    }),
    [
      type,
      plotId,
      workerId,
      seasonSelection,
      search,
      page,
      hasFilters,
      setFilter,
      setPage,
      setSearch,
      clearFilters,
    ]
  );
}
