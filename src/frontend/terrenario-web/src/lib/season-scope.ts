import { useCallback, useState } from 'react';
import { ALL_SEASONS, type SeasonScope } from '../types/season.types';

/**
 * Valor del control cuando el usuario **no ha elegido nada**: el ámbito lo pone el servidor (RN-008).
 * No es «todas»: esa es una elección explícita y tiene su propio valor (`ALL_SEASONS`).
 */
export const SEASON_SCOPE_DEFAULT = '';

export interface SeasonScopeSelection {
  /** Lo que se envía en `season_id`; `undefined` mientras no haya elección explícita. */
  requested: string | undefined;
  /** Lo que debe mostrar el `<select>`: la elección del usuario o, si no la hay, lo que aplicó el servidor. */
  value: string;
  /** Ámbito que el servidor dice haber aplicado. `null` hasta la primera respuesta. */
  applied: SeasonScope | null;
  /** El usuario ha tocado el filtro (para «quitar filtros» y para el rótulo de vacío). */
  isExplicit: boolean;
  /** Nombre de lo que se está viendo, para poder decirlo en pantalla (CA-4). */
  label: string | null;
  select: (value: string) => void;
  /** Devuelve el control al defecto del servidor. */
  reset: () => void;
  /** Registra el ámbito que vino en `meta.scope` de la respuesta. */
  applyFromResponse: (scope: SeasonScope) => void;
}

/**
 * MVP-701 — Estado del filtro de temporada de una vista operativa, con el defecto **resuelto en
 * servidor** (RN-008, `P-082`).
 *
 * La pieza que evita el bucle es separar dos cosas que parecen una: lo que se **pide** (`requested`,
 * vacío mientras el usuario no elija) y lo que se **muestra** (`value`, que cae en lo que el servidor
 * dice haber aplicado). Si la respuesta escribiera en el estado que dispara la petición, cada carga
 * provocaría la siguiente.
 *
 * Vive aquí y no en cada vista porque son tres —diario, cosechas y compras— y la lección de `P-082` es
 * justamente esa: un defecto copiado en varios sitios acaba divergiendo.
 */
export function useSeasonScope(): SeasonScopeSelection {
  const [selection, setSelection] = useState<string>(SEASON_SCOPE_DEFAULT);
  const [applied, setApplied] = useState<SeasonScope | null>(null);

  const isExplicit = selection !== SEASON_SCOPE_DEFAULT;

  const value = isExplicit
    ? selection
    : applied
      ? applied.all_seasons
        ? ALL_SEASONS
        : (applied.season?.id ?? ALL_SEASONS)
      : SEASON_SCOPE_DEFAULT;

  const label = applied ? (applied.all_seasons ? 'todas las campañas' : applied.season?.name ?? null) : null;

  return {
    requested: isExplicit ? selection : undefined,
    value,
    applied,
    isExplicit,
    label,
    select: setSelection,
    reset: useCallback(() => setSelection(SEASON_SCOPE_DEFAULT), []),
    applyFromResponse: useCallback((scope: SeasonScope) => setApplied(scope), []),
  };
}
