import { describe, expect, it } from 'vitest';
// @ts-expect-error -- `scripts/` son módulos de build en JavaScript, fuera del `tsconfig` de la app.
import { construirDocumentoLanding } from '../../scripts/prerenderizar-landings.mjs';

/**
 * MKT-102 — `construirDocumentoLanding` es la parte de `scripts/prerenderizar-landings.mjs` que se
 * puede probar sin arrancar Vite ni tocar disco: recibe la plantilla de `dist/index.html` (aquí, un
 * doble mínimo con la misma forma) y el contenido de una landing, y devuelve el documento final.
 */
const PLANTILLA = `<!doctype html>
<html lang="es">
  <head>
    <meta charset="UTF-8" />
    <title>Terrenario — Tu tierra, bajo control</title>
    <meta
      name="description"
      content="La herramienta sencilla para el agricultor: gestiona terrenos, cosechas, compras y el diario de campo de tu explotación en un solo sitio."
    />
    <link rel="icon" href="/favicon.ico" />
    <link rel="manifest" href="/manifest.webmanifest" />
    <meta property="og:url" content="https://app.terrenario.com/" />
    <meta property="og:title" content="Terrenario — Tu tierra, bajo control" />
    <meta
      property="og:description"
      content="La herramienta sencilla para el agricultor: gestiona terrenos, cosechas, compras y el diario de campo de tu explotación en un solo sitio."
    />
    <link rel="modulepreload" crossorigin href="/assets/react-vendor-abc123.js" />
    <link rel="stylesheet" crossorigin href="/assets/index-abc123.css" />
  </head>
  <body>
    <div id="root"></div>
    <script type="module" crossorigin src="/assets/index-abc123.js"></script>
  </body>
</html>
`;

const CONTENIDO = {
  slug: 'gestion-terrenos',
  path: '/funcionalidades/gestion-terrenos',
  title: 'Gestión de terrenos agrícolas | Terrenario',
  metaDescription: 'Registra cada parcela con propietario, ubicación y número de olivos.',
};

describe('construirDocumentoLanding', () => {
  it('sustituye título, descripción y og:url por los de la landing', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(documento).toContain(`<title>${CONTENIDO.title}</title>`);
    expect(documento).toContain(CONTENIDO.metaDescription);
    expect(documento).not.toContain('Terrenario — Tu tierra, bajo control');
    expect(documento).toContain('content="https://app.terrenario.com/funcionalidades/gestion-terrenos"');
  });

  it('añade un canonical apuntando a la ruta pública de la landing', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(documento).toContain(
      '<link rel="canonical" href="https://app.terrenario.com/funcionalidades/gestion-terrenos" />'
    );
  });

  it('inyecta el cuerpo pre-renderizado dentro de #root', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>hola mundo</main>');

    expect(documento).toContain('<div id="root"><main>hola mundo</main></div>');
  });

  it('no sirve el bundle de la SPA: sin <script type="module"> ni modulepreload', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(documento).not.toMatch(/<script[^>]*type="module"/);
    expect(documento).not.toMatch(/rel="modulepreload"/);
  });

  it('conserva el resto de la cabecera (iconos, manifest, hoja de estilos)', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(documento).toContain('<link rel="icon" href="/favicon.ico" />');
    expect(documento).toContain('<link rel="manifest" href="/manifest.webmanifest" />');
    expect(documento).toContain('href="/assets/index-abc123.css"');
  });
});
