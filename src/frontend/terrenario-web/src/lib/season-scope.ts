import { useCallback, useEffect, useRef, useState } from 'react';
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
/**
 * MVP-705 — Modo **controlado**: quien llama guarda la elección donde quiera. Lo estrena el diario,
 * cuya elección vive en la URL (RN-007) para que un enlace reproduzca lo que veía quien lo comparte.
 * Sin esto, la elección estaría en dos sitios —el estado del hook y la URL— y podrían divergir.
 */
export interface SeasonScopeControl {
  selection: string;
  onSelect: (value: string) => void;
  /**
   * MVP-801 — Cómo se escribe una **corrección** de la selección, cuando el servidor ha aplicado otra
   * cosa. Separada de `onSelect` porque no es una navegación del usuario: si dejara entrada de
   * historial, «atrás» devolvería a la URL con el ámbito ajeno y volvería a corregirse en bucle. Si no
   * se informa, se corrige como una selección normal.
   */
  onCorrect?: (value: string) => void;
}

/** Cómo se llama, en el vocabulario del control, el ámbito que el servidor dice haber aplicado. */
export function appliedSeasonValue(scope: SeasonScope | null): string | null {
  if (!scope) return null;
  return scope.all_seasons ? ALL_SEASONS : (scope.season?.id ?? ALL_SEASONS);
}

export function useSeasonScope(control?: SeasonScopeControl): SeasonScopeSelection {
  const [localSelection, setLocalSelection] = useState<string>(SEASON_SCOPE_DEFAULT);
  const [applied, setApplied] = useState<SeasonScope | null>(null);
  /**
   * Selección que estaba en vigor cuando se registró `applied`. Sin ella no se puede distinguir «el
   * servidor no me hizo caso» de «todavía no ha contestado a lo último que le he pedido», y la segunda
   * situación no debe mover el control.
   */
  const [appliedFor, setAppliedFor] = useState<string | null>(null);

  const selection = control ? control.selection : localSelection;
  const setSelection = control ? control.onSelect : setLocalSelection;
  const correctSelection = control ? (control.onCorrect ?? control.onSelect) : setLocalSelection;

  // La respuesta llega asíncrona: `applyFromResponse` necesita saber qué se estaba pidiendo en ese
  // momento, y una referencia lo dice sin volver a crear la función en cada cambio de filtro (lo que
  // reentraría en el efecto de carga de cada vista).
  const selectionRef = useRef(selection);
  selectionRef.current = selection;

  const isExplicit = selection !== SEASON_SCOPE_DEFAULT;
  const appliedValue = appliedSeasonValue(applied);

  /**
   * MVP-801 (`P-108`) — El servidor ha aplicado **otra cosa** distinta de lo pedido. Ocurre con un
   * `season_id` heredado de otro Workspace: desde `MVP-705` la elección del diario viaja en la URL, y
   * al cambiar de Workspace puede quedar la del anterior. RN-008 hace que el servidor caiga al defecto,
   * pero el control seguía dando por buena la selección explícita; como ese identificador no está entre
   * las opciones, el `<select>` caía en la primera y la pantalla rotulaba «Todas las temporadas»
   * mientras enseñaba una sola campaña. Afirmar un ámbito falso es peor que no decir nada.
   */
  const isOverridden = isExplicit && appliedFor === selection && appliedValue !== null
    && appliedValue !== selection;

  /**
   * Y además se **corrige la URL** (CA-4): se devuelve el control al defecto en vez de reescribirlo con
   * la campaña aplicada. Las dos dejan la pantalla diciendo la verdad, pero solo esta respeta la
   * higiene de RN-007 —los valores por defecto no se escriben—; fijar la campaña resuelta congelaría en
   * el enlace la de trabajo del día en que se corrigió.
   */
  useEffect(() => {
    if (isOverridden) correctSelection(SEASON_SCOPE_DEFAULT);
  }, [isOverridden, correctSelection]);

  const value =
    isExplicit && !isOverridden ? selection : (appliedValue ?? SEASON_SCOPE_DEFAULT);

  const label = applied ? (applied.all_seasons ? 'todas las campañas' : applied.season?.name ?? null) : null;

  return {
    requested: isExplicit ? selection : undefined,
    value,
    applied,
    isExplicit,
    label,
    select: setSelection,
    reset: useCallback(() => setSelection(SEASON_SCOPE_DEFAULT), [setSelection]),
    applyFromResponse: useCallback((scope: SeasonScope) => {
      setApplied(scope);
      setAppliedFor(selectionRef.current);
    }, []),
  };
}
