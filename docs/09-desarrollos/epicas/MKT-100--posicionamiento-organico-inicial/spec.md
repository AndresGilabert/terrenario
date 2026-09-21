---
id: "MKT-100"
tipo: epica
titulo: "Posicionamiento organico inicial"
estado: borrador
prioridad: alta
hito: "Post-MVP — Crecimiento orgánico"
tickets: []
historias: ["MKT-101", "MKT-102", "MKT-103", "MKT-104", "MKT-105", "MKT-106", "MKT-107", "MKT-108", "MKT-109", "MKT-110", "MKT-199"]
depende_de: ["MVP-008"]
bloquea: []
relacionado_con: ["MVP-006", "MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marketing", "seo", "observabilidad", "adquisicion"]
  modulo_path: "03-modulos/"
  componentes: ["landing-publica", "telemetria", "ops-signals", "indexacion"]
  etiquetas: ["post-mvp", "growth", "organic", "first-party-metrics"]
  nivel_riesgo: medio
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# EPICA MKT-100 — Posicionamiento organico inicial

## Contexto

El MVP funcional ya está entregado y el cuello de botella de crecimiento está en la parte pública:
hay poca superficie indexable y falta trazabilidad de adquisición por landing. La épica ordena las
acciones para abrir adquisición orgánica con coste cero, midiendo resultados sin analítica de terceros.

## Objetivo

Construir la base de posicionamiento orgánico inicial con contenido indexable, rastreo técnico,
distribución orgánica y trazabilidad de conversión por landing.

## Requisitos de alto nivel

- Como responsable de crecimiento, quiero conocer qué landing atrae visitas y qué landing convierte,
  para priorizar contenido y mejoras.
- Como responsable de operación, quiero recibir un resumen diario y semanal por correo con métricas
  clave, para no depender de consultas manuales continuas al endpoint de señales.
- Como usuario nuevo, quiero aterrizar en páginas públicas que expliquen funcionalidades reales, para
  decidir si accedo a la plataforma.

## Alcance

- Resumen operativo por email con cadencia diaria y semanal.
- Landings públicas de funcionalidades y casos de uso.
- SEO on-page, schema y rastreo técnico (`robots` + `sitemap`).
- Instrumentación de trazabilidad por landing y conversión completa:
  `landing_view -> login_view -> login_success`.
- Alta en buscadores, baseline y ciclo de optimización.

## Fuera de alcance

- Analítica web de terceros (postergada por ADR-0011).
- Campañas de pago y herramientas de pago.
- Replanteamiento de pricing o cambios de producto fuera del alcance de adquisición orgánica.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en `completado`, incluida `MKT-199`.
- [ ] **CA-2**: Existe superficie pública indexable suficiente (home + landings definidas) con enlazado interno.
- [ ] **CA-3**: La trazabilidad por landing permite medir conversión real por origen de aterrizaje hasta login exitoso.
- [ ] **CA-4**: El equipo recibe resúmenes diario y semanal en el mismo destinatario de alertas (`Ops:AlertEmail`).
