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
beforeEach(() => {
  localStorage.clear();
  sessionStorage.clear();
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});
