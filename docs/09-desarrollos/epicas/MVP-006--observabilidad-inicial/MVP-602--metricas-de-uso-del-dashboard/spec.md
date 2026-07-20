---
id: "MVP-602"
tipo: feature
titulo: "Métricas de uso del dashboard"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-403", "MVP-404", "MVP-405", "MVP-504"]
bloquea: ["MVP-603"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "dashboard"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard-usage", "manual-refresh"]
  etiquetas: ["mvp", "telemetry", "dashboard"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-602 — Métricas de uso del dashboard

## Contexto

El dashboard es la principal promesa de visibilidad del MVP. La KB define KPIs de uso del dashboard y de recarga manual que deben medirse para saber si el producto realmente se consulta y cómo se usa.

## Objetivo

Disponer de señales básicas de uso del dashboard y recarga manual que permitan evaluar adopción y fricción de lectura en los primeros usuarios.

## Requisitos de usuario

### HU-1 — Medir uso del dashboard

**Como** responsable del producto,
**quiero** saber si los usuarios acceden al dashboard y lo recargan,
**para** validar que la capa de visibilidad aporta valor real en el MVP.

## Alcance (in-scope)

- Métricas de acceso al dashboard.
- Señal de recarga manual del dashboard.
- Base mínima para revisión semanal de KPIs de producto.

## Fuera de alcance (out-of-scope)

- Analítica profunda por widget o comportamiento avanzado.
- Heatmaps o tracking exhaustivo de interfaz.

## Criterios de aceptación

- [ ] **CA-1**: El sistema registra el acceso al dashboard de forma suficiente para medir sesiones con uso del panel.
- [ ] **CA-2**: El sistema registra la recarga manual del dashboard como señal separada.
- [ ] **CA-3**: Las métricas permiten revisar los KPIs mínimos de uso definidos en la KB.

## Maquetas y referencias visuales

- Referencia funcional: `kpis.md` y `observabilidad.md`.

## Notas y decisiones

- Esta historia mide adopción mínima, no comportamiento exhaustivo.
