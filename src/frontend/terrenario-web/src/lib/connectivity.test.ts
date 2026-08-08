import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  estadoDeConexion,
  marcarConConexion,
  marcarSinConexion,
  suscribir,
} from './connectivity';

/** MVP-709 (`P-091`) — El estado de conexión y sus dos fuentes. */
describe('connectivity', () => {
  beforeEach(() => {
    marcarConConexion();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    marcarConConexion();
  });

  it('arranca en línea', () => {
    expect(estadoDeConexion()).toBe('en-linea');
  });

  it('pasa a sin conexión cuando una petición muere sin respuesta', () => {
    marcarSinConexion();

    expect(estadoDeConexion()).toBe('sin-conexion');
  });

  it('vuelve a en línea cuando una petición trae respuesta', () => {
    marcarSinConexion();
    marcarConConexion();

    expect(estadoDeConexion()).toBe('en-linea');
  });

  it('escucha los eventos del navegador', () => {
    // El `offline` del navegador llega antes de que nada falle: sirve para avisar sin esperar.
    window.dispatchEvent(new Event('offline'));
    expect(estadoDeConexion()).toBe('sin-conexion');

    // Y el `online` es lo que retira el aviso sin exigir recargar (CA-2).
    window.dispatchEvent(new Event('online'));
    expect(estadoDeConexion()).toBe('en-linea');
  });

  it('no avisa a quien ya sabe el estado', () => {
    // Sin esta guarda, **cada petición correcta** provocaría un render de todo lo suscrito: el estado
    // se confirma en cada respuesta, y son muchas.
    const avisos: string[] = [];
    const desuscribir = suscribir(() => avisos.push(estadoDeConexion()));

    marcarConConexion();
    marcarConConexion();
    marcarSinConexion();
    marcarSinConexion();
    marcarConConexion();

    desuscribir();
    expect(avisos).toEqual(['sin-conexion', 'en-linea']);
  });
});
