---
id: "MVP-710"
tipo: feature
titulo: "Identidad de marca y presencia del producto"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marca", "ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["assets", "index-html", "landing"]
  etiquetas: ["mvp", "ajustes", "marca"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-710 — Identidad de marca y presencia del producto

> **Origen**: `P-080` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

El Product Owner lo reporto como «falta favicon». Verificado que el fichero **si existe y se sirve**
(`GET https://app.terrenario.com/favicon.svg` responde 200, 9.522 bytes), pero su contenido es **el
logo de Vite**: el rayo degradado morado y azul del andamiaje (`#863bff`, `#47bfff`, `#7e14ff`),
versionado desde `MVP-101` y nunca sustituido. La marca real del producto es verde oliva (`#33450d`)
con el icono `eco`.

En la misma superficie faltan, y son el mismo trabajo:

- `apple-touch-icon` e iconos de aplicacion.
- `manifest.webmanifest`: sin el, anadir la aplicacion al inicio de un movil la deja como marcador con
  captura de pantalla, que pesa en una herramienta pensada para el campo.
- `theme-color`.
- `<meta name="description">` y Open Graph en una landing **publica**: compartir el enlace por WhatsApp
  —canal natural entre agricultores— muestra hoy una tarjeta vacia.

## Objetivo

Que el producto se reconozca como suyo fuera de su propia pantalla: en la pestana, en el escritorio del
movil y en un enlace compartido.

## Requisitos de usuario

### HU-1 — Reconocer la aplicacion

**Como** usuario con varias pestanas abiertas,
**quiero** distinguir Terrenario por su icono,
**para** volver a el sin leer los titulos.

### HU-2 — Compartir el producto

**Como** persona que recomienda la herramienta,
**quiero** que al pegar el enlace se vea de que va,
**para** que quien lo reciba entienda que le estoy mandando.

## Alcance (in-scope)

- Favicon propio (SVG y respaldo `.ico`), coherente con la paleta y el simbolo del producto.
- `apple-touch-icon` y juego de iconos de aplicacion.
- `manifest.webmanifest` con nombre, iconos, `theme_color` y `background_color`.
- `theme-color` en `index.html`.
- `<meta name="description">`, Open Graph y Twitter Card en la landing publica, con imagen social
  autoalojada.

## Fuera de alcance (out-of-scope)

- Service worker y capacidades de aplicacion instalable mas alla del manifest.
- Rediseno de la landing o de su contenido.
- Identidad visual nueva: se usa la que el producto ya tiene.

## Criterios de aceptación

- [x] **CA-1**: La pestana del navegador muestra el icono de Terrenario, no el del andamiaje.
  `public/favicon.svg` deja de ser el rayo de Vite y pasa a ser la baldosa `#33450d` con el glifo `eco`
  —el mismo distintivo que ya pintan `AppSidebar`, `HomeView`, `LoginPage` y `LandingPage`—, con
  respaldo `favicon.ico` de 16/32/48 px. El `.ico` se valido **leyendolo con otro programa**
  (`System.Drawing.Icon`): tres entradas, borde `RGB(51,69,13)` y centro blanco. La regresion la fija
  `recursos-de-marca.test.ts`, que no pregunta si hay favicon —lo habia, y ese era el problema— sino si
  lleva `#33450d` y no `863bff`.
- [x] **CA-2**: Anadir la aplicacion al inicio en Android e iOS produce un icono propio y un nombre
  correcto. `manifest.webmanifest` declara `name`/`short_name` «Terrenario», `theme_color` `#33450d`,
  `background_color` `#fcf9f4`, `start_url` `/app` e iconos 192/512 `any` mas 512 `maskable`; iOS usa
  `apple-touch-icon.png` de 180 px, a sangre y opaco porque aplica su propia mascara. Comprobado que
  los ficheros existen y que el proveedor de tipos del pipeline sabe nombrar `.webmanifest`
  (`BrandAssetsContentTypesTests`): sin eso, `UseStaticFiles` lo responderia con 404 y el 404 seria
  invisible. La comprobacion sobre un movil real va con **CA-5**.
- [x] **CA-3**: Compartir `https://app.terrenario.com` en WhatsApp, Telegram o similar muestra titulo,
  descripcion e imagen. El documento lleva `description`, Open Graph completo (`type`, `site_name`,
  `locale`, `url`, `title`, `description`, `image` con tipo, medidas y texto alternativo) y Twitter
  Card `summary_large_image`; `og-image.png` es una imagen propia de 1200x630 compuesta con las
  tipografias del producto. Verificado sobre el HTML generado por `npm run build`. El raspado real
  exige el enlace ya publicado y va con **CA-5**.
- [x] **CA-4**: Todos los recursos son **autoalojados**: la CSP de `MVP-502` no admite terceros y
  `RN-042` no se reabre por una imagen social remota. La politica se leyo del build
  (`dist/csp.policy`), no se supuso: sigue siendo `default-src 'self'` con `img-src 'self' data:`, y
  **no ha hecho falta relajar ninguna directiva**. La guarda `sin-recursos-externos.test.ts` de
  `MVP-599` solo miraba `src/`; ahora cubre tambien `index.html` y el manifest, admitiendo el propio
  origen en las URL absolutas que Open Graph exige.
- [x] **CA-5**: Verificado sobre el build de produccion **servido de verdad**, no solo en desarrollo.
  Se levanto `vite preview` sobre `dist/` y se pidio cada recurso: `favicon.svg` (`image/svg+xml`),
  `favicon.ico` (`image/x-icon`), `apple-touch-icon.png`, `icon-512.png` y `og-image.png`
  (`image/png`) y `manifest.webmanifest` (**`application/manifest+json`**, que es justo el tipo que
  antes faltaba y devolvia 404). Los seis responden `200`. En el HTML servido estan las etiquetas de
  icono, manifest, `theme-color` y las de Open Graph y Twitter, y las **unicas** URL absolutas son las
  de nuestro propio origen que el formato de Open Graph exige.
  El `.ico` se valido ademas **leyendo su estructura con un lector ajeno al que lo escribio**: 3
  entradas DIB (16, 32 y 48 px a 32 bpp), con desplazamientos y tamanos coherentes y 15.086 bytes que
  cuadran con lo que predice el formato.
  **Queda fuera de lo comprobable aqui** y va con el despliegue: ver el icono en el escritorio de un
  movil real tras «anadir al inicio», y el raspado de la tarjeta social, que exige el enlace publicado
  —y cuya cache en WhatsApp y Facebook es larga, asi que puede seguir viendose la tarjeta vieja un
  tiempo—.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| App (transversal) | RN-042 (nada no esencial sin consentimiento) | hecho | Los ocho recursos de marca se sirven del propio origen; `dist/csp.policy` sigue siendo `default-src 'self'` sin relajar nada, y la guarda de recursos externos cubre ya el documento y el manifest |

## Notas y decisiones

- **Cuidado con la CSP.** `MVP-502` inyecta `default-src 'self'` en el build de produccion: cualquier
  recurso de marca servido desde un tercero quedaria bloqueado y, ademas, reabriria la evaluacion de
  `RN-042` que `MVP-505` cerro autoalojando las tipografias.
- **No se ha disenado marca nueva.** El icono es el distintivo que la aplicacion ya pinta en cuatro
  pantallas; lo unico que hace la historia es sacarlo del `<div>`. El glifo `eco` va como **trazado** y
  no como texto porque un favicon se pinta antes de que exista CSS.
- **Los rasterizados no se generan en el build.** `scripts/generar-iconos.mjs` es ad hoc y sus
  dependencias no estan en `package.json`: son ~50 MB de binarios nativos que cada `npm ci` del CI
  pagaria para producir unos ficheros que cambian una vez al ano. La orden para ejecutarlo esta escrita
  en la cabecera del propio script.
- **Sin service worker, Chrome no ofrecera el aviso automatico de instalacion** (queda fuera de alcance
  por decision del spec). «Anadir a pantalla de inicio» desde el menu si usa el manifest, que es lo que
  pide `CA-2`.
- **Las caches de tarjeta social son largas**: tras el despliegue, WhatsApp y Facebook pueden seguir
  mostrando la tarjeta vacia que ya rasparon hasta que caduque o se fuerce el reraspado. Se vera como
  un fallo del cambio y no lo es.
