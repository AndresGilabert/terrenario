---
id: "MVP-710"
tipo: feature
titulo: "Identidad de marca y presencia del producto"
estado: borrador
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
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: La pestana del navegador muestra el icono de Terrenario, no el del andamiaje.
- [ ] **CA-2**: Anadir la aplicacion al inicio en Android e iOS produce un icono propio y un nombre
  correcto.
- [ ] **CA-3**: Compartir `https://app.terrenario.com` en WhatsApp, Telegram o similar muestra titulo,
  descripcion e imagen.
- [ ] **CA-4**: Todos los recursos son **autoalojados**: la CSP de `MVP-502` no admite terceros y
  `RN-042` no se reabre por una imagen social remota.
- [ ] **CA-5**: Verificado sobre el build de produccion servido de verdad, no solo en desarrollo.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| App (transversal) | RN-042 (nada no esencial sin consentimiento) | parcial | El favicon existe pero no es el del producto |

## Notas y decisiones

- **Cuidado con la CSP.** `MVP-502` inyecta `default-src 'self'` en el build de produccion: cualquier
  recurso de marca servido desde un tercero quedaria bloqueado y, ademas, reabriria la evaluacion de
  `RN-042` que `MVP-505` cerro autoalojando las tipografias.
