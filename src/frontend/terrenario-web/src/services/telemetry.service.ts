import { API_BASE } from './api.config';
import { getDeviceType, getEntryReferrer, getSessionId } from '../lib/login-telemetry';
import type { LoginFunnelEventName } from '../lib/login-telemetry';
import type { DashboardWidgetOutcome, UsageEventName } from '../lib/usage-telemetry';

const LOGIN_TELEMETRY_URL = `${API_BASE}/api/v1/auth/telemetry/login`;
const USAGE_TELEMETRY_URL = `${API_BASE}/api/v1/telemetry/usage`;

/**
 * MVP-105 · MVP-601 — Emite un evento del embudo de login originado en el cliente (pantalla vista,
 * clic en Google, abandono). Es fire-and-forget: la telemetría nunca debe frenar ni romper el login.
 *
 * `beacon` usa `navigator.sendBeacon` para los eventos emitidos al abandonar la página (pagehide),
 * donde una petición normal no llegaría a completarse. El resto usa `fetch` con `keepalive` para
 * sobrevivir a la redirección a Google.
 *
 * Las dimensiones `session_id`, `device_type` y `entry_referrer` se resuelven aquí y no las pasa quien
 * llama: son las mismas para todos los eventos, y dejarlas en un solo sitio es lo que impide que un
 * evento salga con ellas y otro sin ellas. El servidor solo usa `entry_referrer` en
 * `login_screen_viewed` (MKT-106); en el resto de eventos lo ignora si llega.
 */
export function logLoginEvent(
  event: LoginFunnelEventName,
  flowId: string,
  options: { beacon?: boolean } = {}
): void {
  const payload = JSON.stringify({
    event,
    flow_id: flowId,
    session_id: getSessionId(),
    device_type: getDeviceType(),
    entry_referrer: getEntryReferrer(),
  });

  if (options.beacon && typeof navigator.sendBeacon === 'function') {
    navigator.sendBeacon(
      LOGIN_TELEMETRY_URL,
      new Blob([payload], { type: 'application/json' })
    );
    return;
  }

  void fetch(LOGIN_TELEMETRY_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: payload,
    keepalive: true,
  }).catch(() => {
    // Silencio deliberado: un fallo de telemetría no debe afectar al usuario.
  });
}

export interface UsageEventPayload {
  firstInSession?: boolean;
  widgets?: readonly DashboardWidgetOutcome[];
}

/**
 * MVP-602 — Emite una señal de uso del producto. El endpoint es autenticado, así que hace falta el
 * token; sin él, la señal simplemente no sale.
 *
 * **No usa el cliente HTTP común, y es deliberado**: ese cliente reacciona a `AUTH_UNAUTHENTICATED`
 * cerrando la sesión, de modo que una llamada de telemetría que llegase con el token justo caducado
 * echaría a la persona de la aplicación. Medir no puede cerrarle la sesión a nadie, así que esta
 * llamada va por su cuenta y se traga cualquier error.
 */
export function logUsageEvent(
  event: UsageEventName,
  accessToken: string | null,
  payload: UsageEventPayload = {}
): void {
  if (!accessToken) return;

  void fetch(USAGE_TELEMETRY_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({
      event,
      session_id: getSessionId(),
      device_type: getDeviceType(),
      first_in_session: payload.firstInSession,
      widgets: payload.widgets,
    }),
    keepalive: true,
  }).catch(() => {
    // Igual que arriba: silencio deliberado.
  });
}
