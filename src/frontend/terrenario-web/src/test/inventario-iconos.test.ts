import { describe, expect, it } from 'vitest';
// @ts-expect-error -- `scripts/` son módulos de build en JavaScript, fuera del `tsconfig` de la app.
import { inventarioDeIconos, spansDeIconos } from '../../scripts/inventario-iconos.mjs';

/**
 * MVP-810 — **El subconjunto de iconos no puede volverse una trampa.**
 *
 * Desde `MVP-810` la fuente de iconos no se sirve entera: el build genera un subconjunto con los
 * glifos que `scripts/inventario-iconos.mjs` encuentra en el código. El riesgo evidente de eso es
 * que alguien escriba un icono de una forma que el inventario no vea: el build no se enteraría, la
 * pantalla saldría con un hueco en blanco, y el síntoma no apunta a la causa. Es el motivo por el
 * que `CA-5` de la historia pide que el proceso esté documentado **y** protegido.
 *
 * La otra mitad de la protección vive en el generador: un nombre que no exista en Material Symbols
 * rompe el `build` en vez de dejar el hueco. Aquí se cubre la mitad que el generador no puede ver,
 * que es un `<span>` cuyo contenido no se puede resolver leyendo el código.
 *
 * Se comprueba sobre el **código fuente**, igual que `sin-recursos-externos.test.ts`: así falla al
 * escribirlo y no al desplegarlo.
 */
type Span = {
  ruta: string;
  linea: number;
  contenido: string;
  nombres: string[];
  resoluble: boolean;
};

describe('inventario de iconos', () => {
  it('todo `<span>` de iconos dice qué glifo pinta', () => {
    const opacos = (spansDeIconos() as Span[])
      .filter((span) => !span.resoluble)
      .map((span) => `${span.ruta}:${span.linea} → ${span.contenido}`);

    expect(
      opacos,
      'El build solo empaqueta los glifos que encuentra en el código. Escribe el nombre como ' +
        'cadena literal dentro del `<span>`, o pásalo por una propiedad `icon` (ver la cabecera ' +
        'de `scripts/inventario-iconos.mjs`). Si no, el icono no se descarga y sale un hueco.'
    ).toEqual([]);
  });

  it('el inventario no está vacío', () => {
    // Una expresión regular rota dejaría el inventario a cero y el subconjunto sin iconos, y los
    // dos casos anteriores pasarían igual: no hay `<span>` irresoluble si no se encuentra ninguno.
    const nombres = inventarioDeIconos() as string[];
    expect(nombres.length).toBeGreaterThan(50);
    expect(nombres).toContain('agriculture');
  });
});
