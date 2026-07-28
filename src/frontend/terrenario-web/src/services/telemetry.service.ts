import { API_BASE } from './api.config';
import type { LoginFunnelEventName } from '../lib/login-telemetry';

const LOGIN_TELEMETRY_URL = `${API_BASE}/api/v1/auth/telemetry/login`;

/**
 * MVP-105 — Emite un evento del embudo de login originado en el cliente (pantalla vista, clic en
 * Google, abandono). Es fire-and-forget: la telemetría nunca debe frenar ni romper el login.
 *
 * `beacon` usa `navigator.sendBeacon` para los eventos emitidos al abandonar la página (pagehide),
 * donde una petición normal no llegaría a completarse. El resto usa `fetch` con `keepalive` para
 * sobrevivir a la redirección a Google.
 */
export function logLoginEvent(
  event: LoginFunnelEventName,
  flowId: string,
  options: { beacon?: boolean } = {}
): void {
  const payload = JSON.stringify({ event, flow_id: flowId });

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
