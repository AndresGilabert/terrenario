import { useEffect, useRef, type RefObject } from 'react';

/**
 * Selector de lo que puede recibir foco con el tabulador. `[hidden]` y `disabled` quedan fuera; el
 * `tabindex="-1"` también, porque es enfocable por código pero no por tabulación.
 */
const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), ' +
  'textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Cuántas capas hay abiertas. El fondo solo vuelve a ser interactivo cuando se cierra la **última**:
 * un `ConfirmDialog` sobre un formulario es un caso real —confirmar el borrado desde el modal de
 * corrección— y con un simple booleano la primera en cerrarse reactivaría el fondo con la otra abierta.
 */
let abiertas = 0;

/** Marca el resto de la aplicación como inerte, o lo devuelve a la vida. */
function fondoInerte(inerte: boolean) {
  const raiz = document.getElementById('root');
  if (!raiz) return;
  if (inerte) raiz.setAttribute('inert', '');
  else raiz.removeAttribute('inert');
  // Sin esto, el fondo se desplaza detrás de la capa al rodar la rueda.
  document.body.style.overflow = inerte ? 'hidden' : '';
}

export interface OpcionesDeCapaModal {
  /** Mientras sea `false` el hook no hace nada: ni apaga el fondo ni escucha teclas. */
  activa: boolean;
  /** El nodo que contiene la capa. De él salen los controles enfocables. */
  contenedor: RefObject<HTMLElement | null>;
  /** `Escape`. La vista decide si de verdad se puede cerrar. */
  onEscape: () => void;
  /** Bloquea `Escape` mientras hay algo en curso que no debe interrumpirse. */
  bloqueada?: boolean;
  /**
   * Qué enfocar al abrir. Por defecto, el primer control del contenedor. `MVP-704` lo usa para
   * saltarse el aspa de cerrar, que es el primero del DOM y convertiría el primer Intro en un cierre.
   */
  elegirFocoInicial?: (enfocables: HTMLElement[]) => HTMLElement | null | undefined;
}

/**
 * MVP-999 (`P-104`) — Convierte un nodo en **el único contexto interactivo de la pantalla**: apaga el
 * fondo, atrapa el foco dentro y lo devuelve a su sitio al cerrar.
 *
 * <b>Por qué es un hook y no una prop más de `Modal`.</b> Estas tres piezas las estrenó `MVP-704`
 * dentro del componente `Modal`, pero **no dependen de su maqueta**: valen igual para un diálogo
 * centrado que para un panel lateral. El *drawer* de navegación de móvil era el último overlay del
 * producto sin trampa de foco (`P-104`) y no cabía en `Modal` —panel a sangre, sin título ni
 * cabecera—; meterlo habría obligado a darle al componente común un modo «lateral», es decir, una
 * segunda personalidad por un solo uso. Extraer lo que no es maqueta resuelve los dos casos y deja la
 * pieza lista para el próximo overlay que tampoco sea un diálogo.
 *
 * Las tres piezas, y hacen falta las tres:
 *
 * 1. **`inert` sobre el resto de la aplicación.** Es lo único que apaga el fondo de una vez: no solo
 *    el tabulador, también el clic, la búsqueda del navegador y el recorrido de un lector de pantalla.
 *    Exige que la capa viva **fuera** del árbol que se apaga —un portal a `body`—; si no, se apagaría
 *    a sí misma.
 * 2. **Trampa de foco.** `inert` ya impide salir, pero sin ciclar el tabulador se va a la barra del
 *    navegador y volver cuesta. Se interviene **solo en los extremos**: en medio, el orden natural ya
 *    es el correcto y reimplementarlo solo introduce diferencias.
 * 3. **Restauración del foco.** Al cerrar vuelve al control que abrió la capa; si no, aterriza en el
 *    `body` y quien navega con teclado tiene que rehacer todo el camino.
 */
export function useCapaModal({
  activa,
  contenedor,
  onEscape,
  bloqueada = false,
  elegirFocoInicial,
}: OpcionesDeCapaModal) {
  // Los tres pueden cambiar entre renders. Se leen por ref, y tienen que ser `useRef` de verdad: con
  // un objeto literal, el efecto se quedaría con el del render en que se montó y no vería los cambios
  // posteriores —el caso que importa es `bloqueada`, que cambia justo al empezar un envío—.
  const escapeRef = useRef(onEscape);
  escapeRef.current = onEscape;
  const bloqueadaRef = useRef(bloqueada);
  bloqueadaRef.current = bloqueada;
  const elegirRef = useRef(elegirFocoInicial);
  elegirRef.current = elegirFocoInicial;

  useEffect(() => {
    if (!activa) return;

    const nodo = contenedor.current;
    const enfocables = () => Array.from(nodo?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? []);

    // Quién tenía el foco antes de abrir. Se guarda **antes** de moverlo.
    const devolverFocoA = document.activeElement as HTMLElement | null;

    abiertas += 1;
    if (abiertas === 1) fondoInerte(true);

    const lista = enfocables();
    (elegirRef.current?.(lista) ?? lista[0] ?? nodo)?.focus();

    const alPulsarTecla = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.stopPropagation();
        if (!bloqueadaRef.current) escapeRef.current();
        return;
      }

      if (event.key !== 'Tab') return;

      const actuales = enfocables();
      if (actuales.length === 0) {
        event.preventDefault();
        return;
      }

      const primero = actuales[0];
      const ultimo = actuales[actuales.length - 1];
      const activo = document.activeElement;

      if (!event.shiftKey && activo === ultimo) {
        event.preventDefault();
        primero.focus();
      } else if (event.shiftKey && activo === primero) {
        event.preventDefault();
        ultimo.focus();
      }
    };

    document.addEventListener('keydown', alPulsarTecla);

    return () => {
      document.removeEventListener('keydown', alPulsarTecla);
      abiertas -= 1;
      if (abiertas === 0) fondoInerte(false);
      // `focus()` sobre un nodo que ya no está en el documento no hace nada, así que se comprueba: el
      // control que abrió la capa puede haber desaparecido con la fila que se acaba de borrar.
      if (devolverFocoA && document.contains(devolverFocoA)) devolverFocoA.focus();
    };
    // `contenedor` es una ref estable; el resto se lee por ref para no rearmar los escuchadores.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activa]);
}

/** Solo para tests: devuelve el contador a cero entre casos. */
export function reiniciarCapasParaTests() {
  abiertas = 0;
  fondoInerte(false);
}
