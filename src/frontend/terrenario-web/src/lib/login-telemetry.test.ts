import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  beginLoginScreen,
  clearLoginFlow,
  getDeviceType,
  getLoginFlowId,
  getSessionId,
  isLoginStarted,
  markLoginStarted,
  restartLoginFlow,
} from './login-telemetry';

/**
 * MVP-601 — Las dos dimensiones nuevas del embudo (`session_id`, `device_type`) y la apertura de un
 * intento nuevo tras un abandono.
 */
describe('login-telemetry', () => {
  beforeEach(() => sessionStorage.clear());

  describe('session_id', () => {
    it('es el mismo durante toda la sesión de navegador', () => {
      expect(getSessionId()).toBe(getSessionId());
    });

    it('no se lleva por delante al cerrar un intento de login', () => {
      // El intento se cierra en cada login; la sesión no. Si el `session_id` muriera con el intento,
      // no se podría responder «de cada sesión que ve el login, ¿cuántas entran?».
      const sessionId = getSessionId();
      beginLoginScreen();
      clearLoginFlow();

      expect(getSessionId()).toBe(sessionId);
    });

    it('es aleatorio y no deriva de nada de la cuenta', () => {
      const sessionId = getSessionId();
      sessionStorage.clear();

      expect(getSessionId()).not.toBe(sessionId);
      expect(sessionId).toMatch(/^[0-9a-f]{32}$/);
    });
  });

  describe('device_type', () => {
    const conPuntero = (coarse: boolean, width: number) => {
      vi.stubGlobal('matchMedia', (query: string) => ({ matches: coarse && query.includes('coarse') }));
      vi.stubGlobal('innerWidth', width);
    };

    afterEach(() => vi.unstubAllGlobals());

    it('es «desktop» cuando el puntero principal es fino', () => {
      // También cubre el portátil con pantalla táctil: tiene puntos táctiles, pero su puntero
      // principal es el ratón.
      conPuntero(false, 1440);

      expect(getDeviceType()).toBe('desktop');
    });

    it('distingue móvil de tableta por el ancho de la ventana', () => {
      conPuntero(true, 390);
      expect(getDeviceType()).toBe('mobile');

      conPuntero(true, 1024);
      expect(getDeviceType()).toBe('tablet');
    });

    it('cae en «desktop» cuando el navegador no sabe responder', () => {
      vi.stubGlobal('matchMedia', undefined);

      expect(getDeviceType()).toBe('desktop');
    });
  });

  describe('intento de login', () => {
    it('conserva el flow_id mientras el intento sigue vivo', () => {
      const flowId = beginLoginScreen();

      expect(beginLoginScreen()).toBe(flowId);
      expect(getLoginFlowId()).toBe(flowId);
    });

    it('abre un flow_id nuevo al reintentar tras un abandono', () => {
      // Reutilizar el mismo id haría que un intento sumara abandono y éxito a la vez, y la conversión
      // del embudo contaría dos veces lo mismo.
      const abandonado = beginLoginScreen();

      const reintento = restartLoginFlow();

      expect(reintento).not.toBe(abandonado);
      expect(getLoginFlowId()).toBe(reintento);
    });

    it('reabre la oportunidad de abandono al reintentar', () => {
      beginLoginScreen();
      markLoginStarted();

      restartLoginFlow();

      expect(isLoginStarted()).toBe(false);
    });
  });
});
