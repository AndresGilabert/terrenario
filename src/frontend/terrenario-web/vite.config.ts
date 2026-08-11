import { resolve } from 'node:path'
import { defineConfig, loadEnv, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
// @ts-expect-error -- scripts/ son módulos de build en JavaScript, fuera del `tsconfig` de la app.
import { generarSubconjuntoDeIconos } from './scripts/subconjunto-iconos.mjs'
// @ts-expect-error -- idem.
import { generarTipografiasDeTexto } from './scripts/tipografias-de-texto.mjs'
// @ts-expect-error -- idem.
import { comprobarUmbrales, informe, medirPrimeraCarga } from './scripts/peso-primera-carga.mjs'

/**
 * MVP-810 — Genera las tipografías que `src/index.css` importa, antes de que nada las referencie.
 *
 * Va en `buildStart` —y no en un `prebuild` de npm— para que valga igual en `vite build` y en
 * `vite dev`: si dependiera del script de npm, quien arrancara Vite a mano se encontraría con que
 * `src/index.css` apunta a un fichero que no existe. Es idempotente, así que arrancar el servidor
 * de desarrollo no paga el recorte más de una vez.
 *
 * Un icono añadido con el servidor de desarrollo en marcha necesita reiniciarlo: el inventario se
 * lee al arrancar. El aviso de que hace falta no es un hueco en blanco, sino el test
 * `iconos-inventario.test.ts` y el fallo del propio generador ante un nombre que no existe.
 */
function tipografiasGeneradas(): Plugin {
  return {
    name: 'terrenario-tipografias-generadas',
    async buildStart() {
      generarTipografiasDeTexto()
      await generarSubconjuntoDeIconos()
    },
  }
}

/**
 * MVP-810 — El `build` falla si la primera carga se pasa del presupuesto.
 *
 * Se mide sobre `dist` ya escrito, que es lo que se publica de verdad, y no sobre el grafo de
 * módulos. Los umbrales y su porqué viven en `scripts/peso-primera-carga.mjs`.
 *
 * Va en `writeBundle` y no en `closeBundle` a propósito: `closeBundle` se ejecuta **también cuando
 * el build ha fallado**, y entonces medía el `dist` de la vez anterior y gritaba por un peso viejo,
 * tapando el error de verdad. `writeBundle` solo corre si se ha escrito algo.
 */
function presupuestoDePrimeraCarga(): Plugin {
  return {
    name: 'terrenario-presupuesto-primera-carga',
    apply: 'build',
    writeBundle() {
      const medida = medirPrimeraCarga(resolve(import.meta.dirname, 'dist'))
      console.log(`\n${informe(medida)}\n`)
      const incumplimientos = comprobarUmbrales(medida)
      if (incumplimientos.length > 0) {
        this.error(
          'El peso de la primera carga se ha pasado del presupuesto (MVP-810):\n' +
            incumplimientos.map((linea: string) => `  - ${linea}`).join('\n') +
            '\n\nSi el aumento está justificado, sube el umbral en `scripts/peso-primera-carga.mjs`' +
            ' y explica en el PR por qué. Lo que no vale es subirlo sin decirlo.'
        )
      }
    },
  }
}

/**
 * MVP-502 — Content-Security-Policy del documento del SPA.
 *
 * La API ya emitía CSP desde MVP-105 (`SecurityHeadersMiddleware`, P-005), pero eso protege poco:
 * sus respuestas son JSON, que no es un contexto de ejecución de scripts. **Donde la CSP mitiga XSS
 * es en el HTML de la aplicación**, y ahí no había ninguna. Importa especialmente aquí porque el
 * token de acceso vive en `sessionStorage`: un script inyectado podría leerlo.
 *
 * Se inyecta **solo en el build de producción**: en desarrollo, Vite necesita scripts en línea para
 * el preámbulo de React Refresh y un WebSocket para el HMR, así que una política estricta rompería
 * el arranque sin proteger nada (el servidor de desarrollo no se expone).
 *
 * `connect-src` incluye el origen real de la API porque el front y el back no comparten origen.
 *
 * `P-067` — Además del `meta`, el plugin emite la política en `csp.policy`, que la API lee para
 * servirla como **cabecera**. Importa por dos motivos: hay directivas que el navegador **ignora en
 * un `meta`** —`frame-ancestors` es una de ellas, y es la que frena el clickjacking— y una cabecera
 * se aplica antes de parsear el documento.
 *
 * Los dos salen de la **misma cadena**, que es el punto: si se generaran por separado acabarían
 * divergiendo y nadie se enteraría hasta que fallara la que no se estaba mirando.
 */
function contentSecurityPolicy(apiBaseUrl: string): Plugin {
  const apiOrigin = (() => {
    try {
      return new URL(apiBaseUrl).origin
    } catch {
      return ''
    }
  })()

  const policy = [
    "default-src 'self'",
    "script-src 'self'",
    // MVP-505 — Las tipografías se autoalojan (RN-042), así que ya no hace falta abrir la política
    // a los dominios de Google: se cierra a 'self'. `'unsafe-inline'` sigue solo por los cinco
    // `style={{ width: … }}` que pintan barras del dashboard con valores calculados.
    "style-src 'self' 'unsafe-inline'",
    "font-src 'self'",
    "img-src 'self' data:",
    `connect-src 'self'${apiOrigin ? ` ${apiOrigin}` : ''}`,
    // La aplicación no embebe nada ni se deja embeber: sin esto, `X-Frame-Options` es el único
    // freno al clickjacking y no lo respetan todos los navegadores.
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "object-src 'none'",
  ].join('; ')

  return {
    name: 'terrenario-csp',
    apply: 'build',

    // La política, también como fichero, para que quien sirva el estático pueda emitirla como
    // **cabecera**. La sirve la propia API (`SecurityHeadersMiddleware`), que la lee de aquí en vez
    // de reescribirla en C#: duplicarla sería la divergencia silenciosa de siempre, y además el
    // backend no conoce el origen que este build inyecta en `connect-src`.
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'csp.policy', source: policy })
    },

    transformIndexHtml: {
      order: 'post',
      handler: (html) =>
        html.replace(
          '<head>',
          `<head>\n    <meta http-equiv="Content-Security-Policy" content="${policy}" />`
        ),
    },
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
    tipografiasGeneradas(),
    tailwindcss(),
    react(),
    contentSecurityPolicy(loadEnv(mode, process.cwd(), 'VITE_').VITE_API_BASE_URL ?? ''),
    presupuestoDePrimeraCarga(),
  ],
  build: {
    // Separa las dependencias que casi nunca cambian (React y el router) del código propio de la app.
    // No reduce la primera carga —el total gzip es el mismo— pero el chunk de vendor se cachea entre
    // despliegues, así que cada versión nueva solo obliga a re-descargar el código de la app (~50 kB
    // gzip) en vez del bundle entero. De paso, ningún chunk supera ya el umbral de aviso de Vite: el
    // aviso desaparece porque el bundle es genuinamente más pequeño, no porque se suba el listón.
    // Rolldown (Vite 8) usa `codeSplitting.groups`; `[\\/]` captura ambos separadores de ruta, que es
    // lo recomendado para no fallar en Windows.
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: 'react-vendor', test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
            { name: 'router', test: /node_modules[\\/]react-router/ },
          ],
        },
      },
    },
  },
}))
