---
id: "MKT-103"
tipo: feature
titulo: "SEO on-page base para landings"
estado: completado
prioridad: alta
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "2d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-102"]
bloquea: ["MKT-104", "MKT-105"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seo", "marketing"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["head-meta", "routing-publico"]
  etiquetas: ["title", "description", "canonical", "hreflang"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-09-01"
---

# MKT-103 — SEO on-page base para landings

## Objetivo

Garantizar metadatos y estructura semántica mínimos por URL pública para indexación limpia.

## Requisitos de usuario

### HU-1 — Aparecer correctamente en resultados de búsqueda

**Como** responsable de crecimiento,
**quiero** que cada landing tenga `title`, `description`, `canonical` y `h1` únicos y coherentes con su intención de búsqueda,
**para** evitar contenido duplicado o mal indexado y mejorar el CTR en buscadores.

### HU-2 — Evitar que las landings compitan entre sí

**Como** responsable de crecimiento,
**quiero** que ninguna landing compita por la misma intención de búsqueda que otra,
**para** que cada URL tenga una oportunidad clara de posicionar sin canibalizarse.

## Alcance (in-scope)

- `title`, `meta description`, `canonical`, `h1` único por landing.
- `hreflang` base para `es-ES`.
- Validación de unicidad y coherencia por URL.

## Criterios de aceptación

- [x] **CA-1**: Cada landing tiene `title` y `description` únicos.
- [x] **CA-2**: Cada landing publica `canonical` autoconsistente.
- [x] **CA-3**: Cada landing tiene un único `h1` alineado con intención principal.
