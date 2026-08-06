// MVP-105 · MVP-601 — Correlación y dimensiones del embudo de login en el cliente.
//
// El flow_id es un correlador aleatorio (sin PII) que acompaña a los eventos del embudo desde que
// se ve la pantalla de login hasta que el acceso termina en éxito o abandono. Vive en sessionStorage
// para sobrevivir a la redirección a Google y volver correlacionado en el callback.
//
// MVP-601 añade las dos dimensiones que la KB exige y no existían: `session_id` (sesión de navegador,
// también aleatoria) y `device_type`. Ninguna de las dos identifica a nadie: no se derivan de la
// cuenta, no salen del sistema y mueren al cerrar la pestaña (RN-020, RN-042).

const FLOW_ID_KEY = 'terrenario_login_flow';
const STARTED_KEY = 'terrenario_login_started';
const SESSION_ID_KEY = 'terrenario_session';

export const LoginFunnelEvent = {
  ScreenViewed: 'login_screen_viewed',
  GoogleClicked: 'login_google_clicked',
  Abandonment: 'login_abandonment',
} as const;

export type LoginFunnelEventName =
  (typeof LoginFunnelEvent)[keyof typeof LoginFunnelEvent];

/**
 * Tiempo sin interacción en la pantalla de login tras el cual el intento se considera abandonado
 * (`observabilidad.md`: el abandono se emite «por timeout de inactividad o cierre/salida sin exito»).
 *
 * 90 s es el doble del objetivo de «tiempo medio de login exitoso» (<= 45 s): lo bastante largo para
 * no llamar abandono a quien está leyendo la pantalla, y lo bastante corto para que la sesión que se
 * queda abierta y olvidada no espere a un `pagehide` que puede no llegar nunca.
 */
export const LOGIN_INACTIVITY_TIMEOUT_MS = 90_000;

function randomId(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
}

/**
 * Identificador aleatorio de la sesión de navegador. Es lo que permite responder «de cada sesión que
 * ve el login, ¿cuántas entran?» sin saber quién es nadie. Se crea al pedirlo por primera vez y muere
 * con la pestaña.
 */
export function getSessionId(): string {
  let sessionId = sessionStorage.getItem(SESSION_ID_KEY);
  if (!sessionId) {
    sessionId = randomId();
    sessionStorage.setItem(SESSION_ID_KEY, sessionId);
  }
  return sessionId;
}

/**
 * Tipo de dispositivo, en la taxonomía cerrada que acepta el servidor. Se deriva de dos señales
 * genéricas —si el puntero principal es grueso y el ancho de la ventana—, no de la cadena de agente
 * de usuario: basta para agrupar el embudo y no se acerca a la huella de dispositivo, que sí exigiría
 * consentimiento.
 *
 * `pointer: coarse` y no `maxTouchPoints`: un portátil con pantalla táctil tiene puntos táctiles pero
 * su puntero principal es el ratón, así que por táctil se colaría como «tablet».
 */
export function getDeviceType(): 'desktop' | 'mobile' | 'tablet' {
  const isCoarsePointer =
    typeof window.matchMedia === 'function' && window.matchMedia('(pointer: coarse)').matches;

  if (!isCoarsePointer) return 'desktop';
  return window.innerWidth < 768 ? 'mobile' : 'tablet';
}

/**
 * Marca la entrada a la pantalla de login: asegura un flow_id para el intento y reinicia el flag de
 * "login iniciado" (cada visita a la pantalla es una nueva oportunidad de abandono). Devuelve el
 * flow_id vigente.
 */
export function beginLoginScreen(): string {
  let flowId = sessionStorage.getItem(FLOW_ID_KEY);
  if (!flowId) {
    flowId = randomId();
    sessionStorage.setItem(FLOW_ID_KEY, flowId);
  }
  sessionStorage.removeItem(STARTED_KEY);
  return flowId;
}

/**
 * Abre un intento **nuevo** sobre la misma sesión. Se usa cuando el anterior ya se dio por abandonado
 * y la persona vuelve a la carga: sin esto, el mismo flow_id acumularía abandono y éxito a la vez y la
 * conversión contaría dos veces el mismo intento.
 */
export function restartLoginFlow(): string {
  const flowId = randomId();
  sessionStorage.setItem(FLOW_ID_KEY, flowId);
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
