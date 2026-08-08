import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach, vi } from 'vitest';

/**
 * MVP-501 — Preparación común de los tests de frontend.
 *
 * Cada test arranca con el DOM, los almacenes del navegador y los dobles de `fetch` limpios: la
 * lógica que se cubre aquí (bandeja de invitaciones, sesión, filtros) guarda estado en
 * `localStorage`/`sessionStorage`, y un test que herede el estado del anterior deja de probar lo que
 * dice probar.
 */
/**
 * MVP-702 — `matchMedia` en jsdom.
 *
 * jsdom no lo implementa, así que cualquier componente que consulte el tamaño de pantalla revienta en
 * los tests aunque funcione en el navegador. Se declara **escritorio** por defecto porque es la forma
 * en la que están escritas las comprobaciones existentes: buscan los controles a la vista, no detrás
 * de un desplegable. Un test que quiera comprobar el móvil sobreescribe este doble.
 *
 * Va en la preparación común y no en cada test para que el siguiente componente que lo use no vuelva
 * a tropezar con lo mismo.
 */
const DESKTOP_WIDTH_PX = 1280;

beforeEach(() => {
  localStorage.clear();
  sessionStorage.clear();

  vi.stubGlobal('matchMedia', (query: string) => {
    const min = /\(min-width:\s*(\d+)px\)/.exec(query);
    const matches = min ? DESKTOP_WIDTH_PX >= Number(min[1]) : false;
    return {
      matches,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    };
  });
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});
