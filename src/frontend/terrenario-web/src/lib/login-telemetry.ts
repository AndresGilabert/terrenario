// MVP-105 — Correlación del embudo de login en el cliente.
//
// El flow_id es un correlador aleatorio (sin PII) que acompaña a los eventos del embudo desde que
// se ve la pantalla de login hasta que el acceso termina en éxito o abandono. Vive en sessionStorage
// para sobrevivir a la redirección a Google y volver correlacionado en el callback.

const FLOW_ID_KEY = 'terrenario_login_flow';
const STARTED_KEY = 'terrenario_login_started';

export const LoginFunnelEvent = {
  ScreenViewed: 'login_screen_viewed',
  GoogleClicked: 'login_google_clicked',
  Abandonment: 'login_abandonment',
} as const;

export type LoginFunnelEventName =
  (typeof LoginFunnelEvent)[keyof typeof LoginFunnelEvent];

function randomFlowId(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
}

/**
 * Marca la entrada a la pantalla de login: asegura un flow_id para el intento y reinicia el flag de
 * "login iniciado" (cada visita a la pantalla es una nueva oportunidad de abandono). Devuelve el
 * flow_id vigente.
 */
export function beginLoginScreen(): string {
  let flowId = sessionStorage.getItem(FLOW_ID_KEY);
  if (!flowId) {
    flowId = randomFlowId();
    sessionStorage.setItem(FLOW_ID_KEY, flowId);
  }
  sessionStorage.removeItem(STARTED_KEY);
  return flowId;
}

export function getLoginFlowId(): string | null {
  return sessionStorage.getItem(FLOW_ID_KEY);
}

/** El usuario pulsó "Continuar con Google": la salida de la página ya no es un abandono. */
export function markLoginStarted(): void {
  sessionStorage.setItem(STARTED_KEY, '1');
}

export function isLoginStarted(): boolean {
  return sessionStorage.getItem(STARTED_KEY) === '1';
}

/** Cierra el intento (éxito): el próximo login abre un flow_id nuevo. */
export function clearLoginFlow(): void {
  sessionStorage.removeItem(FLOW_ID_KEY);
  sessionStorage.removeItem(STARTED_KEY);
}
