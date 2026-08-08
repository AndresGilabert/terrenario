/**
 * MVP-711 — Catálogo cerrado `feedback_kind`. Los valores son vocabulario de negocio y van en
 * español (ADR-0009); tienen que coincidir con `FeedbackKinds` del servidor.
 */
export type FeedbackKind = 'incidencia' | 'sugerencia';

/**
 * Lo que el cliente aporta de un reporte. **Todo lo demás del contexto técnico lo resuelve el
 * servidor**: la versión desplegada la sabe él, y el navegador lo lee de la cabecera de la propia
 * petición. Aquí solo viaja lo que únicamente el cliente conoce.
 */
export interface FeedbackReport {
  kind: FeedbackKind;
  message: string;
  /** `location.pathname`, sin query ni fragmento: los filtros del panel llevan datos del Workspace. */
  path: string;
  /** `X-Request-Id` de la última petición fallida de esta carga de página, si la hubo. */
  last_failed_request_id: string | null;
}
