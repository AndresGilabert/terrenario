import { describe, expect, it } from 'vitest';
import { fechaDeNegocio, fechaDelInstante } from './fechas';

/**
 * MVP-999 (`P-101`) — Al juntar las siete copias resultó que no eran siete copias de lo mismo: había
 * tres formatos y **dos formas de parsear**, una con un defecto latente. Estas pruebas fijan la
 * distinción que importa, que es qué se está formateando y no cómo se ve.
 */
describe('fechaDeNegocio', () => {
  it('formatea un día del calendario', () => {
    expect(fechaDeNegocio('2026-08-09')).toBe('9 ago 2026');
  });

  it('admite día de dos cifras', () => {
    // Es el formato que usa la tarjeta de campaña; se conserva en vez de unificarlo.
    expect(fechaDeNegocio('2026-08-09', { dia: '2-digit' })).toBe('09 ago 2026');
  });

  it('no se desplaza un día por el huso horario', () => {
    // El defecto latente: `new Date('2027-09-01')` es medianoche **UTC**, así que en cualquier huso
    // negativo se pintaba 31 de agosto. Aquí se parsea por componentes, en la zona del usuario.
    const porComponentes = fechaDeNegocio('2027-09-01');
    const local = new Date(2027, 8, 1).toLocaleDateString('es-ES', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });

    expect(porComponentes).toBe(local);
    expect(porComponentes).toContain('1');
    expect(porComponentes).toContain('sept');
  });

  it('devuelve la cadena tal cual si no es una fecha de negocio', () => {
    // Mejor enseñar el dato crudo que «Invalid Date» donde debería ir una fecha.
    expect(fechaDeNegocio('mañana')).toBe('mañana');
    expect(fechaDeNegocio('2026-08-09T18:22:01Z')).toBe('2026-08-09T18:22:01Z');
  });
});

describe('fechaDelInstante', () => {
  it('formatea el día en que cae un instante', () => {
    // La caducidad de una invitación ocurre en un momento concreto, no en un día del calendario.
    // `es-ES` con mes largo intercala las preposiciones: «31 de julio de 2026».
    expect(fechaDelInstante('2026-07-31T18:22:01Z', { mes: 'long' })).toBe('31 de julio de 2026');
  });

  it('devuelve la cadena tal cual si no es un instante legible', () => {
    expect(fechaDelInstante('no-es-una-fecha')).toBe('no-es-una-fecha');
  });
});
