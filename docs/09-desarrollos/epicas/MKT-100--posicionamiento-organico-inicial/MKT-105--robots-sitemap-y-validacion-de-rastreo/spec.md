---
id: "MKT-105"
tipo: feature
titulo: "Robots sitemap y validacion de rastreo"
estado: borrador
prioridad: alta
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "1d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-102", "MKT-103"]
bloquea: ["MKT-107"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seo", "plataforma"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["static-files", "indexacion"]
  etiquetas: ["robots", "sitemap", "crawlability"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# MKT-105 — Robots sitemap y validacion de rastreo

## Objetivo

Exponer directivas de rastreo y mapa de URLs públicas reales para habilitar indexación efectiva.

## Requisitos de usuario

### HU-1 — Permitir que los buscadores rastreen el contenido público

**Como** responsable de crecimiento,
**quiero** que `robots.txt` autorice el rastreo de las páginas públicas y bloquee las privadas,
**para** que los buscadores indexen lo que debe indexarse y no gasten presupuesto de rastreo en rutas privadas.

### HU-2 — Facilitar el descubrimiento de todas las URLs públicas

**Como** responsable de crecimiento,
**quiero** que `sitemap.xml` liste todas las landings públicas vigentes,
**para** acelerar su descubrimiento e indexación por los buscadores.

## Alcance (in-scope)

- `robots.txt` con política para rastreo de páginas públicas.
- `sitemap.xml` con home y landings P0.
- Verificación de respuestas HTTP y accesibilidad de recursos.

## Criterios de aceptación

- [ ] **CA-1**: `GET /robots.txt` responde `200`.
- [ ] **CA-2**: `GET /sitemap.xml` responde `200` e incluye todas las landings públicas P0.
- [ ] **CA-3**: No se listan rutas privadas o no indexables.
