import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * MVP-710 — **Todo lo que el documento y el manifest declaran existe de verdad en `public/`.**
 *
 * Los recursos de marca fallan en silencio. Un `apple-touch-icon` que no está no rompe ninguna
 * pantalla ni escribe nada en ningún log: simplemente iOS pone una captura en el escritorio, y eso
 * solo lo ve quien añade la aplicación al inicio en un móvil, que no es nadie del equipo. Lo mismo
 * con el icono del manifest o con la imagen social, que solo se pide desde el servidor de WhatsApp.
 *
 * Por eso se comprueba aquí y no «mirándolo»: renombrar un fichero de `public/` o cambiar una ruta
 * del manifest tiene que fallar en el commit, no meses después en el móvil de un usuario.
 */
const PROYECTO = join(__dirname, '..', '..');
const PUBLICO = join(PROYECTO, 'public');

/** Rutas absolutas del propio origen (`/favicon.svg`) que aparecen en `href`, `src` o `"src":`. */
function rutasDeclaradas(contenido: string): string[] {
  return [...contenido.matchAll(/(?:href|src|"src")\s*[=:]\s*["'](\/[^"']+)["']/g)].map((m) => m[1]);
}

describe('recursos de marca', () => {
  it('las rutas del documento resuelven a ficheros de public/', () => {
    const documento = readFileSync(join(PROYECTO, 'index.html'), 'utf8');
    // `/src/main.tsx` lo resuelve Vite en el build, no es un fichero de `public/`.
    const rutas = rutasDeclaradas(documento).filter((ruta) => !ruta.startsWith('/src/'));

    expect(rutas).toEqual(
      expect.arrayContaining([
        '/favicon.svg',
        '/favicon.ico',
        '/apple-touch-icon.png',
        '/manifest.webmanifest',
      ])
    );
    expect(rutas.filter((ruta) => !existsSync(join(PUBLICO, ruta)))).toEqual([]);
  });

  it('los iconos del manifest existen y cubren los dos usos', () => {
    const manifest = JSON.parse(readFileSync(join(PUBLICO, 'manifest.webmanifest'), 'utf8'));

    expect(manifest.name).toBe('Terrenario');
    expect(manifest.theme_color).toBe('#33450d');
    expect(manifest.background_color).toBe('#fcf9f4');

    const ausentes = manifest.icons.filter(
      (icono: { src: string }) => !existsSync(join(PUBLICO, icono.src))
    );
    expect(ausentes).toEqual([]);

    // Sin un icono `maskable`, Android recorta el cuadrado dentro de su círculo y se come el glifo.
    const propositos = manifest.icons.map((icono: { purpose: string }) => icono.purpose);
    expect(propositos).toContain('any');
    expect(propositos).toContain('maskable');
  });

  it('el favicon es el de Terrenario y no el del andamiaje de Vite', () => {
    // El defecto que originó la historia (`P-080`) era justo este: el fichero existía y se servía,
    // pero dentro llevaba el rayo morado de Vite. Comprobar que «hay favicon» no habría fallado.
    const favicon = readFileSync(join(PUBLICO, 'favicon.svg'), 'utf8');

    expect(favicon).toContain('#33450d');
    expect(favicon.toLowerCase()).not.toContain('863bff');
  });
});
