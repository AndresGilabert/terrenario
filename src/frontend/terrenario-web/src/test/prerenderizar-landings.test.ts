import { describe, expect, it } from 'vitest';
// @ts-expect-error -- `scripts/` son módulos de build en JavaScript, fuera del `tsconfig` de la app.
import { construirDatosEstructurados, construirDocumentoLanding, construirRobotsTxt, construirSitemapXml } from '../../scripts/prerenderizar-landings.mjs';
import { HOME_META, LANDING_CONTENTS } from '../content/landings';

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
  faqs: [{ question: '¿Qué datos necesito?', answer: 'El nombre y el tipo de propiedad.' }],
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

  it('añade hreflang es-ES apuntando a la misma URL que el canonical', () => {
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(documento).toContain(
      '<link rel="alternate" hreflang="es-ES" href="https://app.terrenario.com/funcionalidades/gestion-terrenos" />'
    );
  });

  it('publica Organization, SoftwareApplication y FAQPage desde las FAQ visibles', () => {
    const datos = construirDatosEstructurados(CONTENIDO);
    const documento = construirDocumentoLanding(PLANTILLA, CONTENIDO, '<main>cuerpo</main>');

    expect(datos['@graph'].map((schema: { '@type': string }) => schema['@type'])).toEqual([
      'Organization',
      'SoftwareApplication',
      'FAQPage',
    ]);
    expect(datos['@graph'][2].mainEntity).toEqual([
      {
        '@type': 'Question',
        name: CONTENIDO.faqs[0].question,
        acceptedAnswer: { '@type': 'Answer', text: CONTENIDO.faqs[0].answer },
      },
    ]);
    expect(documento).toContain('<script type="application/ld+json">');
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

describe('construirRobotsTxt', () => {
  it('permite el rastreo público, excluye las rutas no indexables y anuncia el sitemap', () => {
    const robots = construirRobotsTxt();

    expect(robots).toContain('User-agent: *');
    expect(robots).toContain('Allow: /');
    expect(robots).toContain('Disallow: /app/');
    expect(robots).toContain('Disallow: /onboarding/');
    expect(robots).toContain('Disallow: /invitations/');
    expect(robots).toContain('Disallow: /reactivations/');
    expect(robots).toContain('Disallow: /auth/callback');
    expect(robots).toContain('Disallow: /api/');
    expect(robots).toContain('Sitemap: https://app.terrenario.com/sitemap.xml');
  });
});

describe('construirSitemapXml', () => {
  it('incluye exactamente la home y las diez landings públicas P0', () => {
    const sitemap = construirSitemapXml([HOME_META, ...LANDING_CONTENTS]);
    const urls = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map((match) => match[1]);

    expect(urls).toEqual([
      'https://app.terrenario.com/',
      ...LANDING_CONTENTS.map((content) => `https://app.terrenario.com${content.path}`),
    ]);
    expect(urls).toHaveLength(11);
    expect(sitemap).toContain('<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">');
    expect(sitemap).not.toMatch(/\/(?:app|onboarding|invitations|reactivations|api)(?:\/|<)/);
    expect(sitemap).not.toContain('/auth/callback');
    expect(sitemap).not.toContain('/login');
    expect(sitemap).not.toContain('/legal/');
  });
});
