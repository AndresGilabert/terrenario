---
id: "ADR-0012"
titulo: "Pre-renderizado estático en build para las landings públicas de contenido (MKT-102)"
estado: aceptada
fecha: "2026-08-31"
decisores: ["@andres"]
etiquetas: ["marketing", "seo", "frontend", "rendimiento"]
---

# ADR-0012 — Pre-renderizado estático en build para las landings públicas de contenido

## Estado

`aceptada`

## Contexto

`MKT-102` exige publicar 10 URLs públicas nuevas (`/funcionalidades/*`, `/para/*`) con contenido
indexable por buscadores, antes de que `MKT-105` publique `robots.txt` y `sitemap.xml`.

El frontend es una SPA de React montada por Vite (`ADR-0007`): todo el HTML servido hoy —incluida la
landing actual (`/`)— es el mismo `index.html` con un `<div id="root"></div>` vacío que React rellena
en el navegador. `Program.cs` sirve el cliente desde `wwwroot` (`app.UseStaticFiles()`) y resuelve
cualquier ruta sin fichero físico devolviendo ese mismo `index.html` (`app.MapFallback`, excepto
`/api`), para que el router del cliente decida qué pintar.

Un rastreador que no ejecute JavaScript —o que lo ejecute con presupuesto limitado— vería ese mismo
documento vacío en las 10 URLs nuevas: el contenido «sustantivo» que pide `CA-3` de `MKT-102` no
existiría para quien lo indexa, aunque exista para quien lo visita con el navegador.

Restricciones que limitan las opciones:

- No hay presupuesto para adoptar un framework con SSR/SSG integrado (Next.js, Remix...): sería
  sustituir la base de `ADR-0007`, con impacto en todas las pantallas autenticadas, no solo en 10
  landings públicas.
- `ADR-0011` fija que no hay analítica de terceros ni scripts externos; la CSP del cliente es
  `default-src 'self'` (`MVP-502`). Cualquier solución no puede depender de un servicio externo de
  pre-renderizado (tipo Prerender.io) ni de recursos de terceros.
- `MVP-810` fija un presupuesto de primera carga y falla el `build` si se supera: la solución no
  puede aumentar el peso de la SPA existente.
- El backend (`Program.cs`) sirve el cliente como archivos estáticos y no ejecuta Node ni SSR bajo
  demanda: cualquier renderizado debe resolverse en el `build` del frontend, no en cada petición.

## Decisión

Las 10 landings de `MKT-102`, **y la home pública (`/`)**, se **pre-renderizan a HTML estático en el
`build` del frontend**, sin depender de ningún framework SSR ni de servicios externos:

1. El contenido y el marcado de cada landing viven en un componente React puro sin estado ni
   dependencia de `react-router` (`ContentLandingPage`), porque no tienen ninguna interacción propia
   más allá de enlaces.
2. `scripts/prerenderizar-landings.mjs` usa la API de bajo nivel de Vite (`vite.createServer` en modo
   *middleware* + `ssrLoadModule`, la vía que el propio Vite documenta para pre-renderizado/SSG) para
   ejecutar `ReactDOMServer.renderToStaticMarkup` sobre cada landing tras `vite build`.
3. El HTML final de cada landing sale de la **misma plantilla** que ya genera `vite build`
   (`dist/index.html`): mismos iconos, mismo manifest, misma CSP, mismas fuentes autoalojadas. Solo se
   sustituyen `<title>`, la meta-descripción, `og:url`/`og:title`/`og:description`, se añade un
   `<link rel="canonical">` y se inserta el cuerpo pre-renderizado en `#root`.
4. Cada landing se escribe como fichero físico (`dist/funcionalidades/gestion-terrenos/index.html`,
   etc.), **sin el bundle de JavaScript de la SPA**: como el componente no tiene estado ni
   interacción propia, no hay nada que hidratar, y no descargar React en estas páginas es mejor para
   el rendimiento, no un compromiso.
5. El backend no cambia para las 10 landings: `app.UseStaticFiles()` sirve estos ficheros
   directamente porque existen físicamente en `wwwroot`, antes de que la petición llegue a
   `MapFallback`. La home es la única excepción, y acotada: como `/` es también la ruta que
   `UseDefaultFiles` resolvería contra el `index.html` de siempre (el shell de la SPA, compartido
   por **todas** las demás rutas vía `MapFallback`), un middleware explícito en `Program.cs`
   intercepta `GET /` y sirve `wwwroot/home.html` en su lugar, **antes** de `UseDefaultFiles`. Si
   `home.html` no existe (build de frontend no ejecutado), cae al comportamiento anterior sin
   romper nada. `index.html` sigue siendo exactamente el mismo shell vacío para el resto de rutas:
   sin esta separación, cualquier ruta autenticada (`/app/diario` incluida) mostraría un parpadeo
   de contenido de marketing antes de que React la sustituyera, porque `createRoot(...).render(...)`
   reemplaza `#root` en vez de hidratarlo.
   El pipeline de despliegue tampoco cambia: ya copia `dist/` completo a `wwwroot/` (`deploy.yml`).
6. La navegación **desde** estas páginas —incluida la nueva sección de enlazado de la home
   (`LandingPage`, `CA-3`)— usa `<a href>` planas, nunca `<Link>` de `react-router`: estas rutas no
   están dadas de alta en el router del cliente, y una navegación de React Router hacia una ruta
   ausente ahí caería en el 404 de la SPA en vez de servir la página real. `LandingPage` deja de
   importar `react-router` por completo para poder pre-renderizarse con el mismo mecanismo que las
   10 landings.

## Alternativas consideradas

### Opción A: Servir las landings como rutas cliente más de la SPA (sin cambios)

**Pros**: cero código nuevo de build, mismo patrón que el resto de la aplicación.
**Contras**: no resuelve `CA-3`. El contenido no existe en el HTML que recibe un rastreador sin
ejecutar JavaScript; depender de que Google renderice JS de forma fiable y a tiempo es justo el
riesgo que la historia quiere evitar.

### Opción B: Adoptar un framework SSR/SSG (Next.js, Astro, Remix...)

**Pros**: SSR/SSG de primera clase, ecosistema maduro.
**Contras**: sustituye la base de `ADR-0007` para toda la aplicación, no solo para 10 páginas
públicas; migrar el área autenticada (guardas de sesión, `DataScopeContext`, el cliente HTTP común)
no es proporcional al alcance de `MKT-102` y arriesga romper una SPA que hoy funciona.

### Opción C: Servicio externo de pre-renderizado bajo demanda (tipo Prerender.io)

**Pros**: sin cambios en el `build`, renderiza al vuelo detectando user-agents de rastreador.
**Contras**: depende de un tercero que ejecuta el HTML de la aplicación —incompatible con `ADR-0011`
y con la CSP `default-src 'self'`—, añade coste recurrente y un punto de fallo fuera de nuestro
control para la superficie que más necesita estar disponible para SEO.

### Opción D: Pre-renderizado estático en build con Vite SSR de bajo nivel (elegida)

**Pros**: no introduce ningún framework nuevo ni dependencia adicional (usa `vite` y `react-dom/server`,
ya presentes); no toca el backend ni el pipeline de despliegue; el resultado es HTML completo sin
ninguna dependencia de ejecución de JavaScript, lo mejor posible para indexación y para rendimiento
(sin bundle de React en páginas sin interacción); reutiliza la plantilla de `vite build` en vez de
mantener una paralela.
**Contras**: es una solución a medida y no un patrón de framework con más precedente en el mercado;
si el catálogo de landings creciera mucho, el script de pre-renderizado necesitaría revisarse (hoy es
proporcional a 10 páginas).

## Consecuencias

### Positivas

- `CA-3` de `MKT-102` queda resuelto sin comprometer el presupuesto de primera carga de `MVP-810`: las
  landings nuevas no cuentan contra él porque no cargan el bundle de la SPA.
- La home pública deja de depender de que un rastreador ejecute JavaScript para ver su contenido: es
  indexable en igualdad de condiciones con el resto de landings del plan P0.
- No se abre ninguna dependencia nueva de terceros ni se toca la CSP `default-src 'self'`.
- El backend y el pipeline de despliegue no cambian: las landings son ficheros estáticos más dentro
  de `wwwroot`.
- El patrón queda disponible para `MKT-103`/`MKT-104` (SEO on-page y datos estructurados), que pueden
  seguir enriqueciendo el mismo pipeline de pre-renderizado sin rediseñarlo.

### Negativas / Trade-offs

- Un cambio de contenido en una landing exige un `build` completo del frontend para publicarse; no
  hay edición en caliente. Es aceptable porque el contenido no cambia con frecuencia operativa.
- El script de pre-renderizado es responsabilidad propia del equipo: no hay comunidad de un framework
  detrás si algo falla. Se mitiga con tests sobre la función pura que construye el documento
  (`prerenderizar-landings.test.ts`).

### Neutrales

- Esta decisión no cambia cómo se sirve el resto de la aplicación (área autenticada, login, páginas
  legales): siguen siendo rutas cliente de la SPA servidas por el `index.html` de siempre.

## Referencias

- `../../09-desarrollos/epicas/MKT-100--posicionamiento-organico-inicial/MKT-102--landings-publicas-p0-de-funcionalidades-y-casos/spec.md`
- `../../09-desarrollos/epicas/MKT-100--posicionamiento-organico-inicial/MKT-102--landings-publicas-p0-de-funcionalidades-y-casos/tech-design.md`
- `ADR-0007--frontend-react-typescript-vite-para-mvp.md`
- `ADR-0011--analitica-web-de-terceros-postergada-en-fase-inicial.md`
- `../../09-desarrollos/epicas/MVP-008--ajustes-mvp-02/MVP-810--peso-de-la-primera-carga/tech-design.md`
