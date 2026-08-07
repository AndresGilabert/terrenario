// MVP-602 — Señales de uso del producto.
//
// Mismo criterio que el embudo de login (MVP-601): identificadores aleatorios de sesión, sin PII y sin
// nada que sobreviva a la pestaña. Aquí se reutiliza el **mismo** `session_id`, que es lo que permite
// preguntar «de cada sesión que entra, ¿cuántas abren el dashboard?» en vez de contar visitas sueltas.

const MARKS_KEY = 'terrenario_usage_marks';

export const UsageEvent = {
  AppSessionStarted: 'app_session_started',
  DashboardViewed: 'dashboard_viewed',
  // MVP-706 — `dashboard_manual_refresh` se retira aquí: el botón «Actualizar» era su única fuente y
  // ya no existe (decisión del PO sobre `P-085`). El servidor sigue tolerando el evento para no
  // responder `400` a un cliente cacheado, pero el informe operativo ya no lo publica.
  DashboardWidgets: 'dashboard_widgets',
} as const;

export type UsageEventName = (typeof UsageEvent)[keyof typeof UsageEvent];

export const DashboardWidget = {
  Summary: 'summary',
  KgByDestination: 'kg_by_destination',
  KgByPlot: 'kg_by_plot',
  YieldEvolution: 'yield_evolution',
} as const;

export type DashboardWidgetKey = (typeof DashboardWidget)[keyof typeof DashboardWidget];

/**
 * `empty` **no** es un fallo: el KPI de cobertura de la KB admite expresamente los estados vacíos.
 * Un Workspace que aún no ha cosechado no tiene el dashboard roto.
 */
export type DashboardWidgetStatus = 'ok' | 'empty' | 'error';

export interface DashboardWidgetOutcome {
  widget: DashboardWidgetKey;
  status: DashboardWidgetStatus;
}

/**
 * Marca un hito como ya ocurrido en esta sesión y dice si era la primera vez.
 *
 * Se deduplica **en el cliente** porque es donde vive la sesión: hacerlo en servidor exigiría recordar
 * qué sesiones han pasado por aquí, y esa memoria se perdería en cada reinicio, contando dos veces la
 * misma sesión justo después de cada despliegue.
 */
export function markOnceInSession(mark: string): boolean {
  const marks = new Set((sessionStorage.getItem(MARKS_KEY) ?? '').split(',').filter(Boolean));
  if (marks.has(mark)) return false;

  marks.add(mark);
  sessionStorage.setItem(MARKS_KEY, [...marks].join(','));
  return true;
}

export const UsageMark = {
  AppSession: 'app_session',
  DashboardView: 'dashboard_view',
} as const;
