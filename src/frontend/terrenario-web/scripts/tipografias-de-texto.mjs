/**
 * MVP-810 — Hoja de `@font-face` de las tipografías de texto, sin el formato de reserva.
 *
 * **Qué se encontró al aplicar a `Inter` y `Plus Jakarta Sans` el mismo criterio que a los iconos.**
 * De los 4,93 MB de tipografías que publicaba el build, la primera carga solo pedía 145 kB: el resto
 * no se descargaba nunca. Dos motivos distintos, y solo uno es un defecto:
 *
 *  1. **Los alfabetos que el producto no usa** (cirílico, griego, vietnamita, extensiones latinas).
 *     No se descargan porque `@fontsource` declara cada uno con su `unicode-range` y el navegador
 *     solo pide el que necesita. Eso **no** es servir de más: es cobertura que no cuesta descargas,
 *     y quitarla degradaría en silencio un nombre escrito en otro alfabeto. Se conserva.
 *  2. **La copia `.woff` de reserva.** `@fontsource` declara cada variante dos veces, `woff2`
 *     primero y `woff` después, para navegadores sin `woff2`. Son **655 kB** que duplican bytes que
 *     ya están, en un producto que ya exige mucho más que `woff2` —módulos ES, React 19, Tailwind 4—
 *     así que ningún navegador capaz de ejecutarlo va a pedirlos. Eso sí es servir de más, y es lo
 *     que este script quita.
 *
 * Se **genera** en vez de escribirse a mano para no duplicar los `unicode-range` de `@fontsource`:
 * el día que se actualice el paquete, esta hoja se regenera con lo que traiga y no hay una copia
 * vieja diciendo otra cosa.
 */
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..');
const DIRECTORIO_SALIDA = join(RAIZ, 'src/generado');
const SALIDA = join(DIRECTORIO_SALIDA, 'tipografias-de-texto.css');
const require = createRequire(import.meta.url);

/**
 * Las variantes que el sistema de diseño usa, y solo esas. `Inter` es la tipografía de texto e
 * incluye el peso normal; `Plus Jakarta Sans` es la de titulares y arranca en 500.
 * Comprobado contra las clases de peso que aparecen en el código: 400, 500, 600, 700 y 800.
 */
const VARIANTES = [
  ...[400, 500, 600, 700].map((peso) => `@fontsource/inter/${peso}.css`),
  ...[500, 600, 700, 800].map((peso) => `@fontsource/plus-jakarta-sans/${peso}.css`),
];

/** `url(./files/x.woff2) format('woff2'), url(./files/x.woff) format('woff')` -> solo el primero. */
const RESERVA_WOFF = /\s*,\s*url\([^)]*\.woff\)\s*format\(\s*['"]woff['"]\s*\)/gi;

export function generarTipografiasDeTexto() {
  const bloques = VARIANTES.map((especificador) => {
    const rutaCss = require.resolve(especificador);
    const css = readFileSync(rutaCss, 'utf8').replace(RESERVA_WOFF, '');

    if (/\.woff\)/i.test(css)) {
      throw new Error(`Sigue habiendo una reserva .woff en ${especificador} tras limpiarla.`);
    }

    // Las rutas del paquete son relativas a su propio CSS; aquí pasan a serlo respecto de la hoja
    // generada. Se calculan sobre el fichero ya resuelto, así que da igual dónde esté node_modules.
    return css.replace(/url\((\.\/[^)]+)\)/g, (_, ruta) => {
      const destino = resolve(dirname(rutaCss), ruta);
      return `url(${relative(DIRECTORIO_SALIDA, destino).replace(/\\/g, '/')})`;
    });
  });

  const contenido =
    '/* Generado por scripts/tipografias-de-texto.mjs (MVP-810). No editar: se reescribe en cada\n' +
    '   build a partir de @fontsource. Para cambiar pesos o familias, edita ese script. */\n' +
    bloques.join('\n');

  mkdirSync(DIRECTORIO_SALIDA, { recursive: true });
  writeFileSync(SALIDA, contenido);
  return { variantes: VARIANTES.length, bytes: contenido.length };
}
