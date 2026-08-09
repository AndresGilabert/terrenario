/**
 * MVP-999 (`P-101`) — Formateo de fechas para pantalla, en un solo sitio.
 *
 * Estaba duplicado en **siete** ficheros, y al juntarlos resultó que no eran siete copias de lo mismo:
 * había tres formatos distintos y **dos formas de parsear**, una de ellas con un defecto latente.
 *
 * <b>La distinción que de verdad importa es qué se está formateando</b>, no cómo se ve:
 *
 * - Una **fecha de negocio** (`YYYY-MM-DD`) es un día del calendario del usuario, sin hora ni huso:
 *   la fecha de una labor, de una cosecha o el inicio de una campaña. Se parsea **por componentes**,
 *   nunca con `new Date(iso)`: esa forma la interpreta como medianoche **UTC**, así que en cualquier
 *   huso negativo el 1 de septiembre se pinta como 31 de agosto. En España no se nota —vamos en UTC+1
 *   o +2—, y por eso llevaba tiempo ahí sin dar la cara.
 * - Un **instante** (`2026-07-31T18:22:01Z`) sí lleva hora y huso, y `new Date` es lo correcto: la
 *   caducidad de una invitación ocurre en un momento concreto, no en un día.
 *
 * Se conservan los tres formatos que había en vez de unificarlos: cambiar cómo se ve una fecha en
 * cuatro pantallas no es lo que pedía el punto.
 */

interface OpcionesDeFormato {
  /** `numeric` → «9 ago»; `2-digit` → «09 ago». */
  dia?: 'numeric' | '2-digit';
  /** `short` → «ago»; `long` → «agosto». */
  mes?: 'short' | 'long';
}

/**
 * Formatea una fecha de negocio (`YYYY-MM-DD`) en la zona del usuario.
 *
 * Si la cadena no tiene esa forma se devuelve tal cual: es preferible enseñar el dato crudo a pintar
 * «Invalid Date» donde debería ir una fecha.
 */
export function fechaDeNegocio(iso: string, opciones: OpcionesDeFormato = {}): string {
  const partes = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!partes) return iso;

  const [, anio, mes, dia] = partes;
  return new Date(Number(anio), Number(mes) - 1, Number(dia)).toLocaleDateString('es-ES', {
    day: opciones.dia ?? 'numeric',
    month: opciones.mes ?? 'short',
    year: 'numeric',
  });
}

/** Formatea un instante con hora y huso (`2026-07-31T18:22:01Z`) como el día en que cae. */
export function fechaDelInstante(iso: string, opciones: OpcionesDeFormato = {}): string {
  const momento = new Date(iso);
  if (Number.isNaN(momento.getTime())) return iso;

  return momento.toLocaleDateString('es-ES', {
    day: opciones.dia ?? 'numeric',
    month: opciones.mes ?? 'short',
    year: 'numeric',
  });
}
