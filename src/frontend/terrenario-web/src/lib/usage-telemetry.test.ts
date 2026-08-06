import { beforeEach, describe, expect, it } from 'vitest';
import { UsageMark, markOnceInSession } from './usage-telemetry';

/**
 * MVP-602 — La marca de «ya ocurrido en esta sesión» es lo que separa **sesiones con uso** de
 * **visitas**, que es la diferencia entre el KPI que pide la KB y otro que se le parece.
 */
describe('usage-telemetry', () => {
  beforeEach(() => sessionStorage.clear());

  it('solo dice que sí la primera vez', () => {
    expect(markOnceInSession(UsageMark.DashboardView)).toBe(true);
    expect(markOnceInSession(UsageMark.DashboardView)).toBe(false);
    expect(markOnceInSession(UsageMark.DashboardView)).toBe(false);
  });

  it('lleva cada hito por separado', () => {
    markOnceInSession(UsageMark.AppSession);

    expect(markOnceInSession(UsageMark.DashboardView)).toBe(true);
  });

  it('vuelve a empezar en una sesión nueva', () => {
    // Es lo que hace que el divisor y el numerador del KPI hablen de la misma unidad: la sesión.
    markOnceInSession(UsageMark.DashboardView);
    sessionStorage.clear();

    expect(markOnceInSession(UsageMark.DashboardView)).toBe(true);
  });
});
