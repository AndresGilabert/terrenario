import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginPage } from './LoginPage';
import { LOGIN_INACTIVITY_TIMEOUT_MS, LoginFunnelEvent } from '../../lib/login-telemetry';

const logLoginEvent = vi.fn();

vi.mock('../../services/telemetry.service', () => ({
  logLoginEvent: (...args: unknown[]) => logLoginEvent(...args),
}));

const eventos = () => logLoginEvent.mock.calls.map((call) => call[0] as string);
const abandonos = () => eventos().filter((e) => e === LoginFunnelEvent.Abandonment);

function renderLogin() {
  render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>
  );
}

/**
 * MVP-601 — El abandono por **inactividad**, que es la vía que `observabilidad.md` pedía y no existía.
 * Hasta aquí solo se emitía al salir de la página, así que la pestaña que se queda abierta en el
 * login y a la que nadie vuelve no se contaba como abandono: se perdía del embudo sin más.
 */
describe('LoginPage — traza del embudo', () => {
  beforeEach(() => {
    sessionStorage.clear();
    logLoginEvent.mockClear();
    vi.useFakeTimers();
  });

  afterEach(() => vi.useRealTimers());

  it('emite «pantalla vista» al entrar', () => {
    renderLogin();

    expect(eventos()).toEqual([LoginFunnelEvent.ScreenViewed]);
  });

  it('emite abandono cuando la pantalla se queda quieta', () => {
    renderLogin();

    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS);

    expect(abandonos()).toHaveLength(1);
  });

  it('no emite abandono antes de cumplirse la espera', () => {
    renderLogin();

    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS - 1);

    expect(abandonos()).toHaveLength(0);
  });

  it('no cuenta como abandono a quien está leyendo la pantalla', () => {
    renderLogin();

    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS - 1000);
    window.dispatchEvent(new Event('pointerdown'));
    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS - 1000);

    expect(abandonos()).toHaveLength(0);
  });

  it('no cuenta dos veces el mismo abandono cuando además se cierra la pestaña', () => {
    renderLogin();

    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS);
    window.dispatchEvent(new Event('pagehide'));

    expect(abandonos()).toHaveLength(1);
  });

  it('abre un intento nuevo si vuelve la actividad tras el abandono', () => {
    // Dos intentos, dos «pantalla vista»: así la conversión (éxitos / pantallas vistas) sigue
    // cuadrando en vez de contar un éxito sobre un intento que ya se dio por perdido.
    renderLogin();
    vi.advanceTimersByTime(LOGIN_INACTIVITY_TIMEOUT_MS);
    logLoginEvent.mockClear();

    window.dispatchEvent(new Event('keydown'));

    expect(eventos()).toEqual([LoginFunnelEvent.ScreenViewed]);
    expect(logLoginEvent.mock.calls[0][1]).toEqual(expect.any(String));
  });
});
