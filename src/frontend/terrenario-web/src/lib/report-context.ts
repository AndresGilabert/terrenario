/**
 * MVP-711 — El contexto que el canal de sugerencias e incidencias adjunta al reporte (HU-2).
 *
 * Son las dos cosas que **solo el cliente sabe**: desde qué pantalla se está contando el problema y
 * qué petición falló. El resto del contexto técnico —versión desplegada y navegador— lo resuelve el
 * servidor, que ya lo tiene y no necesita que nadie se lo cuente.
 *
 * **Vive en memoria, no en `sessionStorage`, y es deliberado.** Guardarlo en el navegador añadiría
 * entradas al inventario de tecnologías de almacenamiento que `RN-042` obliga a mantener, y a cambio
 * solo cubriría un caso: fallar, **recargar la página** y reportar después. La aplicación es un SPA,
 * así que ir de la pantalla donde falló algo hasta «Sugerencias e incidencias» no recarga nada y el
 * contexto sigue ahí. Se prefiere perderlo tras un `F5` a ampliar lo que la aplicación guarda en el
 * equipo de quien la usa.
 */

/** Última ruta del área operativa distinta de la del propio canal. */
let lastVisitedPath: string | null = null;

/** `X-Request-Id` de la última respuesta de error. */
let lastFailedRequestId: string | null = null;

/** Ruta del canal: registrarla dejaría siempre «estaba en el formulario de sugerencias». */
const FEEDBACK_PATH = '/app/feedback';

export function recordVisitedPath(pathname: string): void {
  if (pathname !== FEEDBACK_PATH) lastVisitedPath = pathname;
}

/**
 * Anota el `X-Request-Id` de una respuesta de error. Un valor ausente **no borra** el anterior: si
 * la cabecera no llegara —por ejemplo, sin exponer en CORS— es mejor conservar el último
 * identificador conocido que quedarse sin ninguno.
 *
 * Se retiene **solo el identificador**: ni la URL, ni el cuerpo, ni el mensaje. Con eso basta para
 * encontrar la traza, y cualquier otra cosa serían datos de la explotación camino de un buzón de
 * correo.
 */
export function recordFailedRequest(requestId: string | null | undefined): void {
  if (requestId) lastFailedRequestId = requestId;
}

export interface ReportContext {
  /** Dónde estaba quien reporta. `null` si el canal es lo primero que abre en esta carga. */
  path: string | null;
  lastFailedRequestId: string | null;
}

export function getReportContext(): ReportContext {
  return { path: lastVisitedPath, lastFailedRequestId };
}

/** Solo para los tests: el estado es de módulo y sobreviviría entre casos. */
export function resetReportContext(): void {
  lastVisitedPath = null;
  lastFailedRequestId = null;
}
