---
id: "MKT-104"
tipo: feature
titulo: "Datos estructurados y FAQ por landing"
estado: borrador
prioridad: media
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "2d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-102", "MKT-103"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seo", "marketing"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["schema-org", "landing-publica"]
  etiquetas: ["json-ld", "faq", "rich-results"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# MKT-104 — Datos estructurados y FAQ por landing

## Objetivo

Añadir contexto semántico machine-readable para mejorar comprensión de las landings por buscadores.

## Requisitos de usuario

### HU-1 — Mejorar comprensión del contenido por buscadores

**Como** responsable de crecimiento,
**quiero** que las landings publiquen datos estructurados (`Organization`, `SoftwareApplication`, `FAQPage`),
**para** aumentar la probabilidad de aparecer con resultados enriquecidos en buscadores.

### HU-2 — Resolver dudas frecuentes sin salir de la landing

**Como** visitante de una landing,
**quiero** encontrar respuestas a preguntas frecuentes sobre la funcionalidad,
**para** resolver objeciones antes de decidir si accedo a la plataforma.

## Alcance (in-scope)

- Schema `Organization` y `SoftwareApplication` para la superficie pública.
- Schema `FAQPage` en landings con preguntas frecuentes.

## Criterios de aceptación

- [ ] **CA-1**: Las landings publican JSON-LD válido para los tipos definidos.
- [ ] **CA-2**: Cada FAQ incluida en UI está reflejada en su `FAQPage`.
- [ ] **CA-3**: No se introduce contenido estructurado que no exista en la página visible.
