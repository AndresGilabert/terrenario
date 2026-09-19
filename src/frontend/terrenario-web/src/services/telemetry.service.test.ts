import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { logLoginEvent } from './telemetry.service';
import { LoginFunnelEvent } from '../lib/login-telemetry';

/**
 * MVP-105 · MVP-601 · MKT-106 — Lo que importa aquí no es el transporte (`fetch`/`sendBeacon`, ya
 * cubiertos manualmente), sino que el cuerpo lleve las dimensiones mínimas y, desde MKT-106,
 * `entry_referrer` tal cual lo da el navegador.
 */
describe('telemetry.service — logLoginEvent', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('incluye entry_referrer en el cuerpo, tal cual lo da el navegador', () => {
    vi.spyOn(document, 'referrer', 'get').mockReturnValue(
      'https://terrenario.example/funcionalidades/gestion-terrenos'
    );

    logLoginEvent(LoginFunnelEvent.ScreenViewed, 'flow01');

    const [, init] = (fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    const body = JSON.parse(init.body as string);

    expect(body.entry_referrer).toBe('https://terrenario.example/funcionalidades/gestion-terrenos');
  });

  it('manda entry_referrer null cuando no hay referrer', () => {
    vi.spyOn(document, 'referrer', 'get').mockReturnValue('');

    logLoginEvent(LoginFunnelEvent.ScreenViewed, 'flow01');

    const [, init] = (fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    const body = JSON.parse(init.body as string);

    expect(body.entry_referrer).toBeNull();
  });
});
