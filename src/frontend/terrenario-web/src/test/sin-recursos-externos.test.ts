import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * MVP-599 — **Ningún recurso se carga desde un tercero.**
 *
 * No es una preferencia técnica: la Política de Privacidad publicada afirma que las tipografías se
 * sirven desde nuestro propio servidor «para no comunicar tu dirección IP a nadie», y la checklist de
 * cumplimiento declara que no hay transferencias a terceros desde el navegador. Cada `src` externo
 * convierte esas dos frases en falsas.
 *
 * Existe porque se coló una. La landing y el alta de Workspace cargaban una foto de
 * `images.unsplash.com`, y la verificación de `MVP-503` no la vio por dos motivos: buscaba
 * **dominios de Google** en vez de cualquier tercero, y miraba la aplicación autenticada, no la
 * landing. La CSP sí la bloqueó —hizo su trabajo—, pero eso solo se notó al publicar.
 *
 * Se comprueba sobre el **código fuente** y no en el navegador a propósito: así falla al escribirlo,
 * no al desplegarlo, y no depende de qué pantalla se le ocurra visitar a quien revisa.
 */
const RAIZ = join(__dirname, '..');

/**
 * Cargas de subrecursos: las que hacen una petición **sin que la persona lo decida**.
 *
 * Separadas por tipo de fichero a propósito. `url(…)` es sintaxis de CSS, y aplicarla al código
 * marcaba `new URL('https://accounts.google.com/…')` —que construye la **navegación** al login de
 * Google, no carga nada—. Un guardián con falsos positivos se acaba desactivando.
 */
const EN_CODIGO = [
  /\bsrc\s*=\s*["'`]https?:\/\//i,                           // <img src="https://…">
  /\bhref\s*=\s*["'`]https?:\/\/[^"'`]*\.(css|woff2?|ttf)/i, // <link> a fuente u hoja externa
];

const EN_ESTILOS = [
  /\burl\(\s*["']?https?:\/\//i,        // background-image: url(https://…)
  /@import\s+(url\()?["']https?:\/\//i, // @import de CSS
];

function ficheros(directorio: string): string[] {
  return readdirSync(directorio).flatMap((entrada) => {
    const ruta = join(directorio, entrada);
    if (statSync(ruta).isDirectory()) return ficheros(ruta);
    return /\.(tsx?|css)$/.test(entrada) && !/\.test\.tsx?$/.test(entrada) ? [ruta] : [];
  });
}

describe('recursos externos', () => {
  it('ningún fichero carga un subrecurso desde otro dominio', () => {
    const infractores = ficheros(RAIZ).flatMap((ruta) => {
      const patrones = ruta.endsWith('.css') ? EN_ESTILOS : EN_CODIGO;
      const lineas = readFileSync(ruta, 'utf8').split('\n');
      return lineas.flatMap((linea, i) =>
        patrones.some((patron) => patron.test(linea))
          ? [`${ruta.slice(RAIZ.length + 1)}:${i + 1} → ${linea.trim().slice(0, 80)}`]
          : []
      );
    });

    // Los **enlaces** a sitios externos sí son legítimos —`www.aepd.es` en la Política, por ejemplo—:
    // no cargan nada, los sigue la persona si quiere. Lo que aquí no cabe es una petición automática.
    expect(infractores, 'Autoaloja el recurso en `public/` en vez de enlazarlo').toEqual([]);
  });
});
