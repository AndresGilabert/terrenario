---
id: "MVP-006"
tipo: epica
titulo: "Observabilidad inicial"
estado: borrador
prioridad: media
hito: "Hito F — Operación medible"
tickets: []
historias: ["MVP-601", "MVP-602", "MVP-603"]
depende_de: ["MVP-001", "MVP-003", "MVP-004", "MVP-005"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "kpis-producto", "operacion"]
  modulo_path: "03-modulos/"
  componentes: ["telemetria-login", "uso-dashboard", "alertas-basicas"]
  etiquetas: ["mvp", "observability", "telemetry"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# EPICA MVP-006 — Observabilidad inicial

## Contexto

La KB exige medir el embudo de login y los primeros indicadores de uso del dashboard para saber si el MVP realmente reduce fricción y aporta valor. En un equipo pequeño, esta observabilidad debe ser intencional y acotada, no una plataforma de monitoring sobredimensionada.

Esta épica cierra la capacidad de operar y aprender del MVP una vez el producto ya es desplegable.

## Objetivo

Disponer de la telemetría mínima necesaria para detectar abandono en login, uso del dashboard y degradaciones operativas iniciales en los primeros Workspaces activos.

## Requisitos de usuario de alto nivel

- **Como** responsable del producto, **quiero** medir si los usuarios entran y usan el dashboard, **para** validar que el MVP está resolviendo la fricción principal.
- **Como** responsable técnico, **quiero** alertas y señales básicas de degradación, **para** reaccionar rápido sin una operación compleja.

## Alcance

- Telemetría del embudo de login hasta éxito o abandono.
- Métricas de uso del dashboard y recarga manual.
- Alertas básicas de salud operativa alineadas con los KPIs técnicos definidos en KB.
- Señales mínimas para detectar widgets sin datos mostrables o degradaciones funcionales relevantes.

## Fuera de alcance

- Data warehouse o analítica avanzada de producto.
- Observabilidad distribuida compleja o explotación full de trazas.
- Experimentación A/B o analítica de comportamiento avanzada.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: El equipo puede medir de forma trazable el embudo de login y el uso del dashboard sin exponer PII sensible en claro.
- [ ] **CA-3**: Existen alertas básicas o señales equivalentes para detectar degradaciones iniciales del MVP en operación.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-601` — Telemetría mínima del embudo de login.
- `MVP-602` — Métricas de uso del dashboard y recarga manual.
- `MVP-603` — Alertas básicas y señales de degradación inicial.

## Notas y decisiones

- Esta épica no antecede al núcleo MVP; lo acompaña cuando ya existe algo estable que medir.
- Debe mantenerse deliberadamente pequeña para no competir con la entrega funcional principal.
