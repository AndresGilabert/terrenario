---
id: "MKT-106"
tipo: feature
titulo: "Trazabilidad por landing y conversion completa"
estado: en-testing
prioridad: alta
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "3d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-102"]
bloquea: ["MKT-107", "MKT-109", "MKT-110"]
relacionado_con: ["MVP-601", "MVP-602", "ADR-0011"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "marketing", "conversion"]
  modulo_path: "03-modulos/observabilidad"
  componentes: ["telemetry", "ops-signals", "landing-publica", "login-funnel"]
  etiquetas: ["first-party", "utm", "landing-attribution", "conversion"]
  nivel_riesgo: medio
creado_en: "2026-08-31"
actualizado_en: "2026-09-01"
---

# MKT-106 — Trazabilidad por landing y conversion completa

## Contexto

La trazabilidad actual cubre embudo de login y uso de producto, pero no permite atribuir conversión a la
landing de origen.

## Objetivo

Medir por landing el embudo completo de captación:
`landing_view -> login_view -> login_success`.

## Requisitos de usuario

### HU-1 — Medir conversion por landing

**Como** responsable de crecimiento,  
**quiero** ver cuántas visitas a cada landing acaban en login exitoso,  
**para** priorizar contenido y mejoras de conversión con datos reales.

## Alcance (in-scope)

- Evento de visita de landing pública con identificador de landing.
- Correlación con acceso a login y éxito de login.
- Conservación en métricas agregadas de primera parte.
- Inclusión del resumen por landing en señales operativas para consumo interno.

## Fuera de alcance (out-of-scope)

- Perfilado por persona o seguimiento cross-site.
- Analítica de terceros.

## Criterios de aceptación

- [x] **CA-1**: Se registran visitas por landing pública con identificador consistente.
- [x] **CA-2**: Se puede calcular conversión por landing a `login_success`.
- [x] **CA-3**: La medición respeta el modelo de primera parte y agregación sin PII.
