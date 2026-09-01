/**
 * MKT-102 — Pre-renderizado de la home y de las landings públicas de funcionalidades y casos de
 * uso.
 *
 * **Por qué existe.** `ADR-0012` decide servir estas páginas como HTML estático generado en el
 * `build`, en vez de dejarlas como rutas cliente de la SPA: sin esto, un rastreador vería el mismo
 * `<div id="root"></div>` vacío que hoy sirve `MapFallback` para cualquier ruta desconocida, y
 * `CA-3` de `MKT-102` («contenido indexable antes del rastreo técnico») no se cumpliría con
 * JavaScript sin ejecutar.
 *
 * **Por qué la home es un fichero aparte (`home.html`) y no `dist/index.html`.** `index.html` es
 * el documento que `MapFallback` sirve para *cualquier* ruta de la SPA sin fichero físico —
 * `/app/diario` incluida—, porque `React` arranca con `createRoot(...).render(...)` (reemplaza
 * `#root`, no lo hidrata). Si la home se pre-renderizara ahí, cada ruta autenticada mostraría un
 * parpadeo de contenido de marketing antes de que React lo sustituyera. `Program.cs` sirve
 * `home.html` con un middleware propio solo para `GET /`, así que `index.html` sigue siendo el
 * shell vacío de siempre para el resto de rutas.
 *
 * **Cómo lo hace.** No compila un segundo bundle: usa `vite.createServer` en modo *middleware* para
 * cargar `entry-server.tsx` con `ssrLoadModule` (la API de bajo nivel que documenta el propio Vite
 * para pre-render/SSG), invoca `renderToStaticMarkup` por cada página y escribe el resultado sobre
 * la plantilla ya construida en `dist/index.html`. No hay una plantilla paralela que mantener: la
 * cabecera (iconos, manifest, CSP, tipografías) es exactamente la que `vite build` ya generó.
 *
 * Cada página sale **sin el bundle de JavaScript de la SPA**: ni la home ni las landings tienen
 * estado ni interacción propia (son enlaces), así que enviar React y el router sería peso que
 * nadie va a usar.
 *
 * Se ejecuta como paso posterior a `vite build` (`npm run build`, ver `package.json`), nunca antes:
 * necesita que `dist/index.html` y `dist/assets/*.css` ya existan.
 */
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..');
const DIST = join(RAIZ, 'dist');

/** Escapa lo mínimo necesario para meter texto dentro de un atributo o de un nodo HTML. */
function escaparHtml(texto) {
  return texto
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

const ORIGEN = 'https://app.terrenario.com';
const TITULO_HOME = 'Terrenario — Tu tierra, bajo control';
const DESCRIPCION_HOME =
  'La herramienta sencilla para el agricultor: gestiona terrenos, cosechas, compras y el diario de campo de tu explotación en un solo sitio.';

export function construirDatosEstructurados(contenido) {
  const graph = [
    { '@type': 'Organization', name: 'Terrenario', url: ORIGEN },
    {
      '@type': 'SoftwareApplication',
      name: 'Terrenario',
      applicationCategory: 'Agricultural management application',
      operatingSystem: 'Web',
      url: `${ORIGEN}${contenido.path}`,
    },
  ];

  if (contenido.faqs?.length > 0) {
    graph.push({
      '@type': 'FAQPage',
      mainEntity: contenido.faqs.map((faq) => ({
        '@type': 'Question',
        name: faq.question,
        acceptedAnswer: { '@type': 'Answer', text: faq.answer },
      })),
    });
  }

  return {
    '@context': 'https://schema.org',
    '@graph': graph,
  };
}

function serializarDatosEstructurados(contenido) {
  return JSON.stringify(construirDatosEstructurados(contenido)).replace(/</g, '\\u003c');
}

/**
 * Construye el documento HTML final de una landing a partir de la plantilla ya construida
 * (`dist/index.html`) y el cuerpo pre-renderizado del componente.
 *
 * Exportada aparte para poder probarla con datos de ejemplo sin invocar Vite ni tocar disco
 * (`prerenderizar-landings.test.ts`), igual que hace `scripts/peso-primera-carga.mjs`.
 */
export function construirDocumentoLanding(plantillaHtml, contenido, cuerpo) {
  let html = plantillaHtml;

  // El bundle de la SPA no se necesita: la página no tiene estado ni interacción propia.
  html = html.replace(/<script[^>]*type="module"[^>]*>[\s\S]*?<\/script>/g, '');
  html = html.replace(/<link[^>]*rel="modulepreload"[^>]*>\s*/g, '');

  html = html.replaceAll(`<title>${TITULO_HOME}</title>`, `<title>${escaparHtml(contenido.title)}</title>`);
  html = html.replaceAll(escaparHtml(DESCRIPCION_HOME), escaparHtml(contenido.metaDescription));
  html = html.replaceAll(escaparHtml(TITULO_HOME), escaparHtml(contenido.title));
  html = html.replace(
    `<meta property="og:url" content="${ORIGEN}/" />`,
    `<meta property="og:url" content="${ORIGEN}${contenido.path}" />`
  );

  const canonico = `<link rel="canonical" href="${ORIGEN}${contenido.path}" />`;
  const hreflang = `<link rel="alternate" hreflang="es-ES" href="${ORIGEN}${contenido.path}" />`;
  const datosEstructurados = `<script type="application/ld+json">${serializarDatosEstructurados(contenido)}</script>`;
  html = html.replace('<link rel="manifest" href="/manifest.webmanifest" />', (coincidencia) =>
    `${coincidencia}\n    ${canonico}\n    ${hreflang}\n    ${datosEstructurados}`
  );

  html = html.replace('<div id="root"></div>', `<div id="root">${cuerpo}</div>`);

  return html;
}

async function main() {
  const plantilla = readFileSync(join(DIST, 'index.html'), 'utf8');

  // Modo middleware: sin servidor HTTP real, solo la transformación de módulos que Vite ya sabe
  // hacer para SSR. `configFile` reutiliza los plugins existentes (React, Tailwind, tipografías).
  const { createServer } = await import('vite');
  const servidor = await createServer({
    root: RAIZ,
    configFile: join(RAIZ, 'vite.config.ts'),
    server: { middlewareMode: true },
    appType: 'custom',
    logLevel: 'warn',
  });

  try {
    const { renderLandingBody, renderHomeBody } = await servidor.ssrLoadModule('/src/entry-server.tsx');
    const { LANDING_CONTENTS, HOME_META } = await servidor.ssrLoadModule('/src/content/landings.ts');

    // La home primero y a su propio fichero: nunca sobre `dist/index.html` (ver cabecera de este
    // fichero y `ADR-0012`).
    const cuerpoHome = renderHomeBody();
    const documentoHome = construirDocumentoLanding(plantilla, HOME_META, cuerpoHome);
    writeFileSync(join(DIST, 'home.html'), documentoHome);
    console.log('[MKT-102] Portada generada: home.html');

    for (const contenido of LANDING_CONTENTS) {
      const cuerpo = renderLandingBody(contenido);
      const documento = construirDocumentoLanding(plantilla, contenido, cuerpo);
      const rutaSalida = join(DIST, ...contenido.path.split('/').filter(Boolean), 'index.html');

      mkdirSync(dirname(rutaSalida), { recursive: true });
      writeFileSync(rutaSalida, documento);
      console.log(`[MKT-102] Landing generada: ${contenido.path}`);
    }
  } finally {
    await servidor.close();
  }
}

// Solo se ejecuta al invocarlo directamente (`node scripts/prerenderizar-landings.mjs`), nunca al
// importar `construirDocumentoLanding` desde el test. Comparar rutas de fichero resueltas, no el
// `file://` crudo, evita falsos negativos en Windows (unidad en minúsculas, separadores).
if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  main().catch((error) => {
    console.error('[MKT-102] Fallo generando las landings públicas:', error);
    process.exitCode = 1;
  });
}
