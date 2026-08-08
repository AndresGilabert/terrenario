import React, { useCallback, useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';

/**
 * Selector de lo que puede recibir foco con el tabulador. `[hidden]` y `disabled` quedan fuera; el
 * `tabindex="-1"` también, porque es enfocable por código pero no por tabulación.
 */
const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), ' +
  'textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Cuántos modales hay abiertos. El fondo solo vuelve a ser interactivo cuando se cierra el **último**:
 * un `ConfirmDialog` sobre un formulario es un caso real —confirmar el borrado desde el modal de
 * corrección— y con un simple booleano el primero en cerrarse reactivaría el fondo con el otro abierto.
 */
let abiertos = 0;

/** Marca el resto de la aplicación como inerte, o lo devuelve a la vida. */
function fondoInerte(inerte: boolean) {
  const raiz = document.getElementById('root');
  if (!raiz) return;
  if (inerte) raiz.setAttribute('inert', '');
  else raiz.removeAttribute('inert');
  // Sin esto, el fondo se desplaza detrás del diálogo al rodar la rueda.
  document.body.style.overflow = inerte ? 'hidden' : '';
}

export interface ModalProps {
  isOpen: boolean;
  /** Se invoca con `Escape` y al pulsar fuera. La vista decide si de verdad se puede cerrar. */
  onClose: () => void;
  /** Nombre accesible del diálogo. Es lo que anuncia un lector de pantalla al abrirlo. */
  title: string;
  /** Símbolo de Material que acompaña al título en la cabecera por defecto. Decorativo. */
  icon?: string;
  /**
   * Cabecera visible. Si se omite, se pinta una con el `title` y el botón de cerrar; con `null` no se
   * pinta ninguna, para los diálogos que llevan su propio encabezado dentro del cuerpo.
   */
  header?: React.ReactNode;
  /**
   * Bloquea las tres salidas —aspa, `Escape` y clic fuera— mientras hay un envío en curso. Solo el
   * aspa se deshabilitaba antes, así que `Escape` podía descartar un formulario que ya estaba
   * guardándose y dejar al usuario sin saber si se guardó.
   */
  closeDisabled?: boolean;
  /** Ancho máximo del panel; cada modal tenía el suyo y se conserva. */
  panelClassName?: string;
  children: React.ReactNode;
  /**
   * Cerrar al pulsar fuera del panel. Se desactiva donde perder lo escrito por un clic despistado
   * sería caro; el `Escape` y el botón de cerrar siguen estando.
   */
  closeOnBackdrop?: boolean;
}

/**
 * MVP-704 (`P-055`) — Diálogo modal común: **el único contexto interactivo de la pantalla mientras
 * está abierto**, se maneje con ratón, teclado o lector de pantalla.
 *
 * <b>El punto que originó esto se perdió una vez.</b> Se registró en `MVP-304` con destino `MVP-502`,
 * y esa historia se cerró sin hacerlo porque su alcance era seguridad y PII, no accesibilidad: el
 * destino se anotó y nadie lo recogió. Es el motivo del `CA-6` de la épica.
 *
 * <b>No es solo accesibilidad, es un defecto funcional.</b> Hasta aquí el overlay solo tapaba
 * *visualmente*: con un modal abierto, los controles del fondo seguían alcanzándose con el tabulador y
 * seguían pudiendo activarse, así que pulsar el envío del formulario en línea del fondo disparaba el
 * alta equivocada.
 *
 * Tres piezas, y hacen falta las tres:
 *
 * 1. **`inert` sobre el resto de la aplicación.** Es lo que de verdad apaga el fondo: no solo el
 *    tabulador, también el clic, la búsqueda del navegador y el recorrido de un lector de pantalla.
 *    Exige que el diálogo viva **fuera** del árbol que se apaga, de ahí el portal a `body`.
 * 2. **Trampa de foco.** `inert` ya impide salir, pero sin ciclar, el tabulador se va a la barra del
 *    navegador y volver cuesta. Cerrar el ciclo mantiene el manejo dentro del diálogo.
 * 3. **Restauración del foco.** Al cerrar, el foco vuelve al control que lo abrió; si no, aterriza en
 *    el `body` y quien navega con teclado tiene que rehacer todo el camino.
 */
export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  icon,
  header,
  panelClassName = 'max-w-lg',
  children,
  closeOnBackdrop = true,
  closeDisabled = false,
}) => {
  const panelRef = useRef<HTMLDivElement>(null);
  const cuerpoRef = useRef<HTMLDivElement>(null);
  const devolverFocoA = useRef<HTMLElement | null>(null);
  const titleId = useId();

  // El cierre puede cambiar de identidad entre renders; la ref evita rearmar los escuchadores.
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;
  const bloqueadoRef = useRef(closeDisabled);
  bloqueadoRef.current = closeDisabled;

  const enfocables = useCallback(
    () => Array.from(panelRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? []),
    []
  );

  useEffect(() => {
    if (!isOpen) return;

    // Quién tenía el foco antes de abrir. Se guarda **antes** de moverlo.
    devolverFocoA.current = document.activeElement as HTMLElement | null;

    abiertos += 1;
    if (abiertos === 1) fondoInerte(true);

    // Se enfoca el primer control del **cuerpo**, no el primero del panel: el primero del panel es
    // siempre el aspa de cerrar de la cabecera, y dejar ahí el foco al abrir un formulario convierte
    // el primer Intro en un cierre. Si el cuerpo no tiene controles —un aviso solo de texto— se cae al
    // primero del panel, y si tampoco lo hay, al panel.
    // Varios formularios ya declaran `autoFocus` en el campo que quieren primero, y no siempre es el
    // primero del DOM. Se respeta esa intención antes de caer en el orden.
    const cuerpo = cuerpoRef.current;
    const primero =
      cuerpo?.querySelector<HTMLElement>('[autofocus]') ??
      Array.from(cuerpo?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? [])[0] ??
      enfocables()[0] ??
      panelRef.current;
    primero?.focus();

    const alPulsarTecla = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.stopPropagation();
        if (!bloqueadoRef.current) onCloseRef.current();
        return;
      }

      if (event.key !== 'Tab') return;

      const lista = enfocables();
      if (lista.length === 0) {
        event.preventDefault();
        return;
      }

      const primeroDeLaLista = lista[0];
      const ultimo = lista[lista.length - 1];
      const activo = document.activeElement;

      // Solo se interviene en los extremos: en medio, el tabulador del navegador ya hace lo correcto.
      if (!event.shiftKey && activo === ultimo) {
        event.preventDefault();
        primeroDeLaLista.focus();
      } else if (event.shiftKey && activo === primeroDeLaLista) {
        event.preventDefault();
        ultimo.focus();
      }
    };

    document.addEventListener('keydown', alPulsarTecla);

    return () => {
      document.removeEventListener('keydown', alPulsarTecla);
      abiertos -= 1;
      if (abiertos === 0) fondoInerte(false);
      // `focus()` sobre un nodo que ya no está en el documento no hace nada, así que se comprueba.
      const destino = devolverFocoA.current;
      if (destino && document.contains(destino)) destino.focus();
    };
  }, [isOpen, enfocables]);

  if (!isOpen) return null;

  const conCabeceraPropia = header !== undefined;

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs"
      // El clic fuera cierra, pero solo si nació **y** murió en el fondo: arrastrar una selección de
      // texto desde dentro del panel y soltar fuera no es un clic fuera, y cerrar ahí perdería lo
      // escrito.
      onMouseDown={(event) => {
        if (closeOnBackdrop && !closeDisabled && event.target === event.currentTarget) onClose();
      }}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        // Con la cabecera por defecto el nombre sale del `h3` que ya se ve. Con una a medida se pone
        // como `aria-label`: el diálogo tiene que tener nombre igual, y así no hace falta un texto
        // oculto que duplique el que ya está en pantalla.
        {...(conCabeceraPropia ? { 'aria-label': title } : { 'aria-labelledby': titleId })}
        tabIndex={-1}
        className={`bg-white rounded-2xl w-full ${panelClassName} border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col outline-none`}
      >
        {conCabeceraPropia ? (
          header
        ) : (
          <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between gap-3 shrink-0">
            <div className="flex items-center gap-2 min-w-0">
              {icon && (
                <span className="material-symbols-outlined text-[#33450d] text-xl shrink-0" aria-hidden="true">
                  {icon}
                </span>
              )}
              <h3 id={titleId} className="font-headline font-bold text-lg text-[#1c1c19] truncate">
                {title}
              </h3>
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={closeDisabled}
              aria-label="Cerrar"
              className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#e5e2dd] disabled:opacity-60 transition-colors shrink-0"
            >
              <span className="material-symbols-outlined" aria-hidden="true">close</span>
            </button>
          </div>
        )}
        {/* `contents` no crea caja: el envoltorio existe solo para poder distinguir el cuerpo de la
            cabecera al elegir el foco inicial, y los hijos siguen siendo hijos directos del panel
            flex, que es de lo que dependen sus alturas y sus zonas desplazables. */}
        <div ref={cuerpoRef} className="contents">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
};
