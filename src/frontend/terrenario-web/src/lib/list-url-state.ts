import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';

/** Valor de un filtro de lista cuando no acota nada. No viaja a la URL. */
export const FILTER_ALL = 'todos';

/**
 * Declaración de los parámetros de una vista: qué filtros tiene, cómo se llaman en la URL y qué valor
 * suyo significa «sin filtro».
 */
export interface ListUrlSpec {
  /** Filtro lógico → nombre del parámetro y el valor que **no** se escribe. */
  filters: Record<string, { param: string; fallback: string }>;
  /** Nombre del parámetro de búsqueda rebotada. Omitido si la vista no busca. */
  search?: string;
  /** Nombre del parámetro de página. Omitido si la vista no pagina. */
  page?: string;
}

export interface ListUrlState {
  /** Valor vigente de cada filtro declarado, ya con su defecto aplicado. */
  values: Record<string, string>;
  /** Término **ya aplicado**. Lo que se está tecleando lo guarda la vista. */
  search: string;
  page: number;
  /** ¿Hay algo puesto a mano? Lo usa el rótulo de vacío y el botón de quitar filtros. */
  hasFilters: boolean;
  /** Cuántos filtros hay puestos, para el contador del desplegable en móvil. */
  activeCount: number;

  /**
   * Cambia uno o varios filtros: vuelve a la página 1 y **deja entrada de historial**.
   *
   * Con `{ replace: true }` la **sustituye**. Lo usa la corrección de un ámbito que el servidor no ha
   * podido aplicar (`P-108`, MVP-801): no es una navegación del usuario, y dejar entrada haría que
   * «atrás» devolviera a la URL con el ámbito ajeno y volviera a corregirse en bucle.
   */
  setFilter: (patch: Record<string, string>, options?: { replace?: boolean }) => void;
  /** Cambia de página, con entrada de historial: «atrás» devuelve a la anterior. */
  setPage: (page: number) => void;
  /**
   * Fija el término ya rebotado. **Sustituye** la entrada de historial en vez de añadir una: si no,
   * escribir «sulfatado» dejaría una entrada por pulsación y el botón «atrás» quedaría inservible.
   */
  setSearch: (term: string) => void;
  clearFilters: () => void;
}

/**
 * MVP-705 (`P-072`) · MVP-802 (`P-109`) — Estado de navegación de una vista operativa, con la **URL
 * como fuente única** (RN-007).
 *
 * No es una regla nueva: `RN-007` ya exigía conservar los filtros en la recarga, y `MVP-405` la
 * materializó en la URL para el dashboard. `MVP-705` la aplicó al diario, que es donde más duele —cinco
 * filtros y paginación—, y `MVP-802` a las dos vistas que faltaban: cosechas y compras. Con eso las
 * cuatro vistas operativas se comportan igual ante la misma acción del usuario.
 *
 * **La pieza es una sola a propósito.** La lección de `P-082` —y antes de `P-072`— es que un
 * comportamiento copiado en varias vistas acaba divergiendo; aquí ya tenía tres candidatos a divergir.
 * Lo que cambia por vista es la **declaración** de sus parámetros, no la mecánica.
 *
 * Dos invariantes sostienen el diseño:
 *
 * 1. **Los valores por defecto no ensucian la URL.** «Todos», la página 1 y la búsqueda vacía se
 *    **borran** del parámetro en vez de escribirse. Y la temporada por defecto tampoco aparece: desde
 *    `MVP-701` la resuelve el servidor (RN-008), así que fijarla en la URL congelaría la campaña de
 *    trabajo del día en que se compartió el enlace.
 * 2. **La búsqueda sustituye la entrada de historial; el resto la añade.** Es lo que permite que
 *    «atrás» devuelva al estado anterior de filtros sin que teclear genere una entrada por carácter.
 */
export function useListUrlState(spec: ListUrlSpec): ListUrlState {
  const [searchParams, setSearchParams] = useSearchParams();

  // La declaración es un literal en el cuerpo de cada vista, así que cambia de identidad en cada
  // render: se depende de su **contenido**, que sí es estable.
  const specKey = JSON.stringify(spec);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const stableSpec = useMemo(() => spec, [specKey]);

  const values = useMemo(() => {
    const result: Record<string, string> = {};
    for (const [key, { param, fallback }] of Object.entries(stableSpec.filters)) {
      result[key] = searchParams.get(param) ?? fallback;
    }
    return result;
  }, [searchParams, stableSpec]);

  const search = stableSpec.search ? (searchParams.get(stableSpec.search) ?? '') : '';

  // Una página que no es un entero positivo es basura en la URL, no un error del usuario: se cae a la
  // primera en vez de pedir una página −3 al servidor.
  const rawPage = stableSpec.page ? Number(searchParams.get(stableSpec.page)) : 1;
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
    (patch: Record<string, string>, options?: { replace?: boolean }) => {
      const next: Record<string, string | null> = {};
      for (const [key, value] of Object.entries(patch)) {
        const declared = stableSpec.filters[key];
        if (!declared) continue;
        next[declared.param] = value === declared.fallback ? null : value;
      }

      // Cualquier cambio de filtro vuelve a la primera página: seguir en la 4 de una lista que acaba
      // de reducirse a 12 entradas dejaría la pantalla vacía sin explicar por qué. Va en la **misma**
      // escritura que el filtro, no en un efecto aparte, para que no salga una petición intermedia
      // con el filtro nuevo y la página vieja.
      if (stableSpec.page) next[stableSpec.page] = null;

      write(next, { replace: options?.replace ?? false });
    },
    [write, stableSpec]
  );

  const setPage = useCallback(
    (value: number) => {
      if (!stableSpec.page) return;
      write({ [stableSpec.page]: value <= 1 ? null : String(value) }, { replace: false });
    },
    [write, stableSpec]
  );

  const setSearch = useCallback(
    (term: string) => {
      if (!stableSpec.search) return;
      const next: Record<string, string | null> = { [stableSpec.search]: term || null };
      if (stableSpec.page) next[stableSpec.page] = null;
      write(next, { replace: true });
    },
    [write, stableSpec]
  );

  const clearFilters = useCallback(() => {
    const next: Record<string, string | null> = {};
    for (const { param } of Object.values(stableSpec.filters)) next[param] = null;
    if (stableSpec.search) next[stableSpec.search] = null;
    if (stableSpec.page) next[stableSpec.page] = null;
    write(next, { replace: false });
  }, [write, stableSpec]);

  const activeCount =
    Object.entries(stableSpec.filters).filter(([key, { fallback }]) => values[key] !== fallback)
      .length + (search !== '' ? 1 : 0);

  return useMemo(
    () => ({
      values,
      search,
      page,
      hasFilters: activeCount > 0,
      activeCount,
      setFilter,
      setPage,
      setSearch,
      clearFilters,
    }),
    [values, search, page, activeCount, setFilter, setPage, setSearch, clearFilters]
  );
}
