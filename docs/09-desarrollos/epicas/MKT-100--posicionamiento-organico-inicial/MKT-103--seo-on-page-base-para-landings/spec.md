---
id: "MKT-103"
tipo: feature
titulo: "SEO on-page base para landings"
estado: borrador
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
actualizado_en: "2026-08-31"
---

# MKT-103 — SEO on-page base para landings

## Objetivo

Garantizar metadatos y estructura semántica mínimos por URL pública para indexación limpia.

## Alcance (in-scope)

- `title`, `meta description`, `canonical`, `h1` único por landing.
- `hreflang` base para `es-ES`.
- Validación de unicidad y coherencia por URL.

## Criterios de aceptación

- [ ] **CA-1**: Cada landing tiene `title` y `description` únicos.
- [ ] **CA-2**: Cada landing publica `canonical` autoconsistente.
- [ ] **CA-3**: Cada landing tiene un único `h1` alineado con intención principal.
