---
id: "MVP-403"
tipo: feature
titulo: "Dashboard resumen y kg por destino"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401", "MVP-402"]
bloquea: ["MVP-405"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "resumen", "kg-por-destino"]
  etiquetas: ["mvp", "dashboard", "kpi"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-403 — Dashboard resumen y kg por destino

## Contexto

El dashboard MVP debe empezar por el bloque de lectura más directo: resumen de temporada y reparto de kilos por destino. Estos widgets convierten la producción ya registrada en una vista rápida y comparable por Workspace.

## Objetivo

Mostrar un resumen de temporada útil y un desglose fiable por destino a partir de las cosechas registradas en el MVP.

## Requisitos de usuario

### HU-1 — Ver un resumen claro de temporada

**Como** usuario que revisa resultados,
**quiero** ver los principales indicadores de la temporada,
**para** entender de un vistazo el estado productivo del Workspace.

### HU-2 — Ver el reparto por destino

**Como** usuario del Workspace,
**quiero** consultar los kg agrupados por destino,
**para** entender cómo se está distribuyendo la producción registrada.

## Alcance (in-scope)

- Widget de resumen de temporada.
- Cálculo de `kg_total`, `litros_total` cuando exista dato y `rendimiento_medio`.
- Widget de kg por destino respetando taxonomía cerrada.
- Inclusión de categoría `desconocido` con alias visual permitido.

## Fuera de alcance (out-of-scope)

- Comparativas analíticas avanzadas por múltiples dimensiones.
- Cuadros de mando financieros.
- Drill-down complejo fuera de los filtros MVP.

## Criterios de aceptación

- [ ] **CA-1**: El dashboard muestra un resumen de temporada basado en las cosechas del Workspace y temporada activa o seleccionada.
- [ ] **CA-2**: El widget de kg por destino usa la taxonomía cerrada del MVP, incluyendo `desconocido`.
- [ ] **CA-3**: Los widgets se muestran sin error bloqueante incluso cuando algunos datos complementarios no existan.

## Maquetas y referencias visuales

- Referencia funcional: RN-009 y KPIs de resumen/destino.

## Notas y decisiones

- Esta historia cubre dos de los cuatro widgets MVP.